using Microsoft.Data.Sqlite;
using System.Text.Json;

namespace Program
{
    public class Coord
    {
        public double? lat { get; set; }
        public double? lon { get; set; }
    }

    public class City
    {
        public int? id { get; set; }
        public string? name { get; set; }
        public Coord? coord { get; set; }
        public string? country { get; set; }
        public int? population { get; set; }
        public int? timezone { get; set; }
        public int? sunrise { get; set; }
        public int? sunset { get; set; }
    }

    public class WeatherInfoMain
    {
        public double? temp { get; set; }
        public double? feels_like { get; set; }
        public double? temp_min { get; set; }
        public double? temp_max { get; set; }
        public int? pressure { get; set; }
        public int? sea_level { get; set; }
        public int? grnd_level { get; set; }
        public int? humidity { get; set; }
        public double? temp_kf { get; set; }
    }

    public class WeatherInfoWeatherItem
    {
        public int? id { get; set; }
        public string? main { get; set; }
        public string? description { get; set; }
        public string? icon { get; set; }
    }

    public class WeatherInfoWind
    {
        public double? speed { get; set; }
        public int? deg { get; set; }
        public double? gust { get; set; }
    }

    public class WeatherInfo
    {
        public int? dt { get; set; }
        public WeatherInfoMain? main { get; set; }
        public WeatherInfoWind? wind { get; set; }
        public IList<WeatherInfoWeatherItem>? weather { get; set; }
        public string? dt_txt { get; set; }
    }

    public class WeatherForecast
    {
        public string? cod { get; set; }
        public int? message { get; set; }
        public int? cnt { get; set; }
        public City? city { get; set; }
        public IList<WeatherInfo>? list { get; set; }
    }

    class Program
    {
        private static readonly HttpClient client = new HttpClient();
        private static string url = "https://api.openweathermap.org/data/2.5/forecast?appid=d306da81e4dd5234cef576d817c3564b&q=Cherkasy&cnt=5";
        private static SqliteConnection sqlite_conn;

        static async Task Main(string[] args)
        {
            sqlite_conn = CreateConnection();

            CreateTable(sqlite_conn);

            await GetWeather();

            Console.WriteLine("Hello, World!");
        }

        static async Task GetWeather()
        {
            try
            {
                Console.WriteLine("Getting JSON...");
                var responseString = await client.GetStringAsync(url);
                Console.WriteLine("Parsing JSON...");
                WeatherForecast? weatherForecast =
                   JsonSerializer.Deserialize<WeatherForecast>(responseString);
                Console.WriteLine($"cod: {weatherForecast?.cod}");
                Console.WriteLine($"City: {weatherForecast?.city?.name}");
                Console.WriteLine($"list count: {weatherForecast?.list?.Count}");
                foreach (var weatherInfo in weatherForecast?.list)
                {
                    Console.WriteLine($"weather temp: {weatherInfo?.main?.temp}");
                    Console.WriteLine($"weather humidity: {weatherInfo?.main?.humidity}");

                    foreach (var weatherInfoWeather in weatherInfo?.weather)
                    {
                        Console.WriteLine($"  weather main: {weatherInfoWeather?.main}");
                        Console.WriteLine($"  weather description: {weatherInfoWeather?.description}");
                    }
                }

                InsertData(weatherForecast?.list);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while getting the weather: " + ex.Message);
            }
        }

        static SqliteConnection CreateConnection()
        {
            SqliteConnection sqlite_conn;
            try
            {
                sqlite_conn = new SqliteConnection("Data Source = database_weather.db");
                sqlite_conn.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error creating database connection: " + ex.Message);
                sqlite_conn = null;
            }
            return sqlite_conn;
        }

        static void CreateTable(SqliteConnection conn)
        {
            try
            {
                using (SqliteCommand sqlite_cmd = conn.CreateCommand())
                {
                    sqlite_cmd.CommandText = "CREATE TABLE IF NOT EXISTS WeatherTable (dt INTEGER, temp REAL, humidity INTEGER)";
                    sqlite_cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error creating table: " + ex.Message);
            }
        }

        static void InsertData(IList<WeatherInfo>? weatherInfoList)
        {
            try
            {
                using (SqliteCommand sqlite_cmd = sqlite_conn.CreateCommand())
                {
                    sqlite_cmd.CommandText = "INSERT INTO WeatherTable (dt, temp, humidity) VALUES (@dt, @temp, @humidity)";

                    foreach (var weatherInfo in weatherInfoList)
                    {
                        sqlite_cmd.Parameters.Clear();

                        sqlite_cmd.Parameters.AddWithValue("@dt", weatherInfo?.dt);
                        sqlite_cmd.Parameters.AddWithValue("@temp", weatherInfo?.main?.temp);
                        sqlite_cmd.Parameters.AddWithValue("@humidity", weatherInfo?.main?.humidity);
                        sqlite_cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error inserting data: " + ex.Message);
            }
        }
    }
}
