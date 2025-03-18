//using System.Net.Http;
//using System.Net.Http.Json;
//using System.Threading;
//using System.Threading.Tasks;

//public class UserActivityService : IAsyncDisposable
//{
//    private readonly HttpClient _httpClient;
//    private PeriodicTimer? _timer;
//    private CancellationTokenSource? _cts;
//    private bool _isUpdating = false;

//    public UserActivityService(IHttpClientFactory httpClientFactory)
//    {
//        _httpClient = httpClientFactory.CreateClient("IdentityAPI");
//    }

//    public void StartTracking()
//    {
//        Console.WriteLine("In UserActivityService _isUpdating: " + _isUpdating);

//        if (_timer != null)
//        {
//            Console.WriteLine("⏳ Timer already running, skipping re-initialization.");
//            return;
//        }

//        _cts = new CancellationTokenSource();
//        _timer = new PeriodicTimer(TimeSpan.FromMinutes(5)); // ✅ Replaces System.Timers.Timer

//        Console.WriteLine("✅ UserActivityService Timer Started.");

//        // Start tracking loop in the background
//        _ = TrackUserActivityAsync(_cts.Token);
//    }

//    private async Task TrackUserActivityAsync(CancellationToken cancellationToken)
//    {
//        try
//        {
//            while (await _timer.WaitForNextTickAsync(cancellationToken)) // ✅ Proper async timer
//            {
//                await UpdateLastActivity();
//            }
//        }
//        catch (OperationCanceledException)
//        {
//            Console.WriteLine("⚠️ UserActivityService tracking canceled.");
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"⚠️ UserActivityService encountered an error: {ex.Message}");
//        }
//    }

//    private async Task UpdateLastActivity()
//    {
//        Console.WriteLine("In UpdateLastActivity started tracking"); // Debugging log

//        if (_isUpdating)
//        {
//            Console.WriteLine("⚠️ Skipping UpdateLastActivity, already updating.");
//            return;
//        }

//        _isUpdating = true;

//        try
//        {
//            var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/update-activity");

//            Console.WriteLine("🔹 Sending request to update activity.");

//            var response = await _httpClient.SendAsync(request);
//            var responseText = await response.Content.ReadAsStringAsync();

//            Console.WriteLine($"🔹 Status Code: {response.StatusCode}");
//            Console.WriteLine($"🔹 Response: {responseText}");

//            if (response.IsSuccessStatusCode)
//            {
//                Console.WriteLine("✅ User last activity updated.");
//            }
//            else
//            {
//                Console.WriteLine($"⚠️ Failed to update last activity. Status: {response.StatusCode}");
//            }
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"⚠️ Failed to update last activity: {ex.Message}");
//        }
//        finally
//        {
//            _isUpdating = false;
//        }
//    }

//    public async ValueTask DisposeAsync()
//    {
//        if (_cts != null)
//        {
//            _cts.Cancel();
//            _cts.Dispose();
//        }
//    }
//}
