using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using SchoolAdmission.Application.DTOs;
using SchoolAdmission.Domain.Enums;
using SchoolAdmission.Tests.Fixtures;
using Xunit;

namespace SchoolAdmission.Tests.IntegrationTests;

public class AdmissionIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AdmissionIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Can_Create_Class_And_Retrieve_Details()
    {
        // Arrange
        var request = new CreateClassRequest
        {
            Name = "Class 6 - Morning Shift",
            TotalSeats = 120
        };

        // Act
        var createResponse = await _client.PostAsJsonAsync("/classes", request);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdClass = await createResponse.Content.ReadFromJsonAsync<ClassResponse>(JsonOptions);
        createdClass.Should().NotBeNull();
        createdClass!.Id.Should().StartWith("c_");
        createdClass.Name.Should().Be("Class 6 - Morning Shift");
        createdClass.TotalSeats.Should().Be(120);

        // Get single class details
        var getResponse = await _client.GetAsync($"/classes/{createdClass.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var details = await getResponse.Content.ReadFromJsonAsync<ClassDetailsResponse>(JsonOptions);
        details.Should().NotBeNull();
        details!.Id.Should().Be(createdClass.Id);
        details.TotalSeats.Should().Be(120);
        details.HeldSeats.Should().Be(0);
        details.ConfirmedSeats.Should().Be(0);
        details.AvailableSeats.Should().Be(120);
    }

    [Fact]
    public async Task Test1_Can_Hold_Available_Seat_Successfully()
    {
        // Arrange: Create a class
        var classResponse = await _client.PostAsJsonAsync("/classes", new CreateClassRequest
        {
            Name = "Class 7 - Day Shift",
            TotalSeats = 50
        });
        var createdClass = await classResponse.Content.ReadFromJsonAsync<ClassResponse>(JsonOptions);

        var studentId = "20152691234567890";
        var request = new HttpRequestMessage(HttpMethod.Post, $"/classes/{createdClass!.Id}/holds");
        request.Headers.Add("X-Student-Id", studentId);

        // Act
        var holdResponse = await _client.SendAsync(request);

        // Assert
        holdResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var hold = await holdResponse.Content.ReadFromJsonAsync<HoldResponse>(JsonOptions);
        hold.Should().NotBeNull();
        hold!.HoldId.Should().StartWith("h_");
        hold.ClassId.Should().Be(createdClass.Id);
        hold.StudentId.Should().Be(studentId);
        hold.Status.Should().Be(HoldStatus.Held);
        hold.ExpiresAt.Should().NotBeNullOrWhiteSpace();

        // Check class statistics
        var classDetails = await _client.GetFromJsonAsync<ClassDetailsResponse>($"/classes/{createdClass.Id}", JsonOptions);
        classDetails!.HeldSeats.Should().Be(1);
        classDetails.ConfirmedSeats.Should().Be(0);
        classDetails.AvailableSeats.Should().Be(49);
    }

    [Fact]
    public async Task Test2_Expired_Hold_Frees_Seat_And_Cannot_Be_Confirmed()
    {
        try
        {
            // Arrange: Create class with 1 seat
            var classResponse = await _client.PostAsJsonAsync("/classes", new CreateClassRequest
            {
                Name = "Class 8 - Single Seat",
                TotalSeats = 1
            });
            var createdClass = await classResponse.Content.ReadFromJsonAsync<ClassResponse>(JsonOptions);

            var student1 = "20152691000000001";
            var holdReq1 = new HttpRequestMessage(HttpMethod.Post, $"/classes/{createdClass!.Id}/holds");
            holdReq1.Headers.Add("X-Student-Id", student1);
            var holdRes1 = await _client.SendAsync(holdReq1);
            holdRes1.StatusCode.Should().Be(HttpStatusCode.Created);
            var hold = await holdRes1.Content.ReadFromJsonAsync<HoldResponse>(JsonOptions);

            // Verify seat is currently held
            var statsBefore = await _client.GetFromJsonAsync<ClassDetailsResponse>($"/classes/{createdClass.Id}", JsonOptions);
            statsBefore!.AvailableSeats.Should().Be(0);
            statsBefore.HeldSeats.Should().Be(1);

            // Act: Advance clock by 601 seconds (past the 600s expiration)
            _factory.Clock.Advance(TimeSpan.FromSeconds(601));

            // Assert 1: Seat is immediately available without any manual sweeper
            var statsAfter = await _client.GetFromJsonAsync<ClassDetailsResponse>($"/classes/{createdClass.Id}", JsonOptions);
            statsAfter!.AvailableSeats.Should().Be(1);
            statsAfter.HeldSeats.Should().Be(0);

            // Assert 2: Hold details shows EXPIRED
            var holdDetails = await _client.GetFromJsonAsync<HoldDetailsResponse>($"/holds/{hold!.HoldId}", JsonOptions);
            holdDetails!.Status.Should().Be(HoldStatus.Expired);

            // Assert 3: Student 1 CANNOT confirm the expired hold
            var confirmReq = new HttpRequestMessage(HttpMethod.Post, $"/holds/{hold.HoldId}/confirm");
            confirmReq.Headers.Add("X-Student-Id", student1);
            var confirmRes = await _client.SendAsync(confirmReq);
            confirmRes.StatusCode.Should().Be(HttpStatusCode.Conflict);

            // Assert 4: Another student can now hold the freed seat
            var student2 = "20152691000000002";
            var holdReq2 = new HttpRequestMessage(HttpMethod.Post, $"/classes/{createdClass.Id}/holds");
            holdReq2.Headers.Add("X-Student-Id", student2);
            var holdRes2 = await _client.SendAsync(holdReq2);
            holdRes2.StatusCode.Should().Be(HttpStatusCode.Created);
        }
        finally
        {
            _factory.Clock.Reset();
        }
    }

    [Fact]
    public async Task Test3_Concurrency_Test_Parallel_Requests_Never_Overbook()
    {
        // Arrange: Create class with only 5 seats
        const int totalSeats = 5;
        const int concurrentApplicants = 35; // 35 simultaneous applicants competing for 5 seats

        var classResponse = await _client.PostAsJsonAsync("/classes", new CreateClassRequest
        {
            Name = "Class 9 - High Demand",
            TotalSeats = totalSeats
        });
        var createdClass = await classResponse.Content.ReadFromJsonAsync<ClassResponse>(JsonOptions);

        // Prepare 35 distinct 17-digit student IDs
        var applicantIds = Enumerable.Range(1, concurrentApplicants)
            .Select(i => $"201526999999{i:D5}")
            .ToList();

        // Act: Fire all 35 requests simultaneously in parallel
        var tasks = applicantIds.Select(async studentId =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/classes/{createdClass!.Id}/holds");
            request.Headers.Add("X-Student-Id", studentId);
            return await _client.SendAsync(request);
        });

        var responses = await Task.WhenAll(tasks);

        // Assert
        var successfulHolds = responses.Where(r => r.StatusCode == HttpStatusCode.Created).ToList();
        var rejectedHolds = responses.Where(r => r.StatusCode == HttpStatusCode.Conflict).ToList();

        successfulHolds.Count.Should().Be(totalSeats, "Exactly the available number of seats should be reserved.");
        rejectedHolds.Count.Should().Be(concurrentApplicants - totalSeats, "All excess applicants must receive 409 Conflict.");

        // Verify class statistics: heldSeats + confirmedSeats must equal totalSeats, never greater
        var stats = await _client.GetFromJsonAsync<ClassDetailsResponse>($"/classes/{createdClass!.Id}", JsonOptions);
        stats!.TotalSeats.Should().Be(totalSeats);
        stats.HeldSeats.Should().Be(totalSeats);
        stats.ConfirmedSeats.Should().Be(0);
        stats.AvailableSeats.Should().Be(0);
        (stats.HeldSeats + stats.ConfirmedSeats).Should().BeLessOrEqualTo(stats.TotalSeats);
    }

    [Fact]
    public async Task Confirmation_Is_Idempotent_And_Does_Not_Double_Count()
    {
        // Arrange: Create class and hold seat
        var classRes = await _client.PostAsJsonAsync("/classes", new CreateClassRequest
        {
            Name = "Class 10 - Idempotency",
            TotalSeats = 10
        });
        var createdClass = await classRes.Content.ReadFromJsonAsync<ClassResponse>(JsonOptions);

        var studentId = "20152695555555555";
        var holdReq = new HttpRequestMessage(HttpMethod.Post, $"/classes/{createdClass!.Id}/holds");
        holdReq.Headers.Add("X-Student-Id", studentId);
        var holdRes = await _client.SendAsync(holdReq);
        var hold = await holdRes.Content.ReadFromJsonAsync<HoldResponse>(JsonOptions);

        // Act 1: First confirmation
        var confirmReq1 = new HttpRequestMessage(HttpMethod.Post, $"/holds/{hold!.HoldId}/confirm");
        confirmReq1.Headers.Add("X-Student-Id", studentId);
        var confirmRes1 = await _client.SendAsync(confirmReq1);
        confirmRes1.StatusCode.Should().Be(HttpStatusCode.OK);

        var confirmBody1 = await confirmRes1.Content.ReadFromJsonAsync<ConfirmHoldResponse>(JsonOptions);
        confirmBody1!.Status.Should().Be(HoldStatus.Confirmed);

        var stats1 = await _client.GetFromJsonAsync<ClassDetailsResponse>($"/classes/{createdClass.Id}", JsonOptions);
        stats1!.ConfirmedSeats.Should().Be(1);
        stats1.HeldSeats.Should().Be(0);
        stats1.AvailableSeats.Should().Be(9);

        // Act 2: Second confirmation (Idempotent test)
        var confirmReq2 = new HttpRequestMessage(HttpMethod.Post, $"/holds/{hold.HoldId}/confirm");
        confirmReq2.Headers.Add("X-Student-Id", studentId);
        var confirmRes2 = await _client.SendAsync(confirmReq2);
        confirmRes2.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert: Confirmed count must NOT increase
        var stats2 = await _client.GetFromJsonAsync<ClassDetailsResponse>($"/classes/{createdClass.Id}", JsonOptions);
        stats2!.ConfirmedSeats.Should().Be(1);
        stats2.HeldSeats.Should().Be(0);
        stats2.AvailableSeats.Should().Be(9);
    }

    [Fact]
    public async Task Cancellation_Releases_Held_Seat()
    {
        // Arrange
        var classRes = await _client.PostAsJsonAsync("/classes", new CreateClassRequest
        {
            Name = "Class 6 - Cancellation Test",
            TotalSeats = 5
        });
        var createdClass = await classRes.Content.ReadFromJsonAsync<ClassResponse>(JsonOptions);

        var studentId = "20152697777777777";
        var holdReq = new HttpRequestMessage(HttpMethod.Post, $"/classes/{createdClass!.Id}/holds");
        holdReq.Headers.Add("X-Student-Id", studentId);
        var holdRes = await _client.SendAsync(holdReq);
        var hold = await holdRes.Content.ReadFromJsonAsync<HoldResponse>(JsonOptions);

        // Act: Cancel hold
        var cancelReq = new HttpRequestMessage(HttpMethod.Delete, $"/holds/{hold!.HoldId}");
        cancelReq.Headers.Add("X-Student-Id", studentId);
        var cancelRes = await _client.SendAsync(cancelReq);
        cancelRes.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert: Seat is released
        var stats = await _client.GetFromJsonAsync<ClassDetailsResponse>($"/classes/{createdClass.Id}", JsonOptions);
        stats!.HeldSeats.Should().Be(0);
        stats.AvailableSeats.Should().Be(5);

        // Hold status is CANCELLED
        var holdDetails = await _client.GetFromJsonAsync<HoldDetailsResponse>($"/holds/{hold.HoldId}", JsonOptions);
        holdDetails!.Status.Should().Be(HoldStatus.Cancelled);
    }

    [Fact]
    public async Task Ownership_Validation_Rejects_Other_Students()
    {
        // Arrange: Student A holds seat
        var classRes = await _client.PostAsJsonAsync("/classes", new CreateClassRequest
        {
            Name = "Class 6 - Ownership",
            TotalSeats = 5
        });
        var createdClass = await classRes.Content.ReadFromJsonAsync<ClassResponse>(JsonOptions);

        var studentA = "20152691111111111";
        var studentB = "20152692222222222";

        var holdReq = new HttpRequestMessage(HttpMethod.Post, $"/classes/{createdClass!.Id}/holds");
        holdReq.Headers.Add("X-Student-Id", studentA);
        var holdRes = await _client.SendAsync(holdReq);
        var hold = await holdRes.Content.ReadFromJsonAsync<HoldResponse>(JsonOptions);

        // Act 1: Student B attempts to confirm Student A's hold
        var confirmReq = new HttpRequestMessage(HttpMethod.Post, $"/holds/{hold!.HoldId}/confirm");
        confirmReq.Headers.Add("X-Student-Id", studentB);
        var confirmRes = await _client.SendAsync(confirmReq);
        confirmRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Act 2: Student B attempts to cancel Student A's hold
        var cancelReq = new HttpRequestMessage(HttpMethod.Delete, $"/holds/{hold.HoldId}");
        cancelReq.Headers.Add("X-Student-Id", studentB);
        var cancelRes = await _client.SendAsync(cancelReq);
        cancelRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Duplicate_Active_Hold_In_Same_Class_Is_Rejected()
    {
        // Arrange
        var classRes = await _client.PostAsJsonAsync("/classes", new CreateClassRequest
        {
            Name = "Class 6 - Duplicate Prevention",
            TotalSeats = 10
        });
        var createdClass = await classRes.Content.ReadFromJsonAsync<ClassResponse>(JsonOptions);

        var studentId = "20152693333333333";

        // First hold succeeds
        var req1 = new HttpRequestMessage(HttpMethod.Post, $"/classes/{createdClass!.Id}/holds");
        req1.Headers.Add("X-Student-Id", studentId);
        var res1 = await _client.SendAsync(req1);
        res1.StatusCode.Should().Be(HttpStatusCode.Created);

        // Second hold for same student in same class must fail with 409 Conflict
        var req2 = new HttpRequestMessage(HttpMethod.Post, $"/classes/{createdClass.Id}/holds");
        req2.Headers.Add("X-Student-Id", studentId);
        var res2 = await _client.SendAsync(req2);
        res2.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Invalid_Student_Id_Returns_BadRequest_Never_500()
    {
        var classRes = await _client.PostAsJsonAsync("/classes", new CreateClassRequest
        {
            Name = "Class 6 - Validation",
            TotalSeats = 10
        });
        var createdClass = await classRes.Content.ReadFromJsonAsync<ClassResponse>(JsonOptions);

        // Missing header
        var reqMissing = new HttpRequestMessage(HttpMethod.Post, $"/classes/{createdClass!.Id}/holds");
        var resMissing = await _client.SendAsync(reqMissing);
        resMissing.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Invalid length (10 digits instead of 17)
        var reqShort = new HttpRequestMessage(HttpMethod.Post, $"/classes/{createdClass.Id}/holds");
        reqShort.Headers.Add("X-Student-Id", "1234567890");
        var resShort = await _client.SendAsync(reqShort);
        resShort.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Contains letters
        var reqAlpha = new HttpRequestMessage(HttpMethod.Post, $"/classes/{createdClass.Id}/holds");
        reqAlpha.Headers.Add("X-Student-Id", "2015269123456789X");
        var resAlpha = await _client.SendAsync(reqAlpha);
        resAlpha.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Can_Retrieve_Student_Applications_For_Refresh_Restoration()
    {
        // Arrange
        var classRes = await _client.PostAsJsonAsync("/classes", new CreateClassRequest
        {
            Name = "Class 6 - Student Holds",
            TotalSeats = 10
        });
        var createdClass = await classRes.Content.ReadFromJsonAsync<ClassResponse>(JsonOptions);

        var studentId = "20152698888888888";
        var holdReq = new HttpRequestMessage(HttpMethod.Post, $"/classes/{createdClass!.Id}/holds");
        holdReq.Headers.Add("X-Student-Id", studentId);
        await _client.SendAsync(holdReq);

        // Act: Call GET /me/holds
        var meReq = new HttpRequestMessage(HttpMethod.Get, "/me/holds");
        meReq.Headers.Add("X-Student-Id", studentId);
        var meRes = await _client.SendAsync(meReq);

        // Assert
        meRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var holds = await meRes.Content.ReadFromJsonAsync<List<StudentHoldResponse>>(JsonOptions);
        holds.Should().NotBeNull();
        holds!.Count.Should().Be(1);
        holds[0].ClassId.Should().Be(createdClass.Id);
        holds[0].ClassName.Should().Be("Class 6 - Student Holds");
        holds[0].Status.Should().Be(HoldStatus.Held);
    }
}
