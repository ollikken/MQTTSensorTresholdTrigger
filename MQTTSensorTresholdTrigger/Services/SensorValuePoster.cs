using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using MQTTSensorTresholdTrigger.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace MQTTSensorTresholdTrigger.Services
{
    public class SensorValuePoster
    {
        private HttpClient client;

        private string url;
        //The url used for posting sensor values
        public string Url
        {
            get { return url; }
        }

        public SensorValuePoster(string url)
        {
            this.url = url;
            this.client = new HttpClient();
        }

        public async void PostTresholdValueExceeded(SensorValue sensorValue)
        {
            var postObject = new SensorValuePost(sensorValue,
                SensorValuePosterMessages.Messages[SensorValuePosterMessagesIndexes.ThresholdExceeded]);

            var json = JsonConvert.SerializeObject(postObject,new JsonSerializerSettings
            {
                DateFormatString = "yyyy-MM-ddTHH:mm:ss.ffffff", //have to use custom format because Newtonsoft.Json uses Z to denote UTC on end of datetime string when using ISO
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
            });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var result = await client.PostAsync(Url, content);
            if(!result.IsSuccessStatusCode)
                Console.WriteLine($"SensorValuePoster failed to post {SensorValuePosterMessagesIndexes.ThresholdExceeded}");
        }
    }

    public enum SensorValuePosterMessagesIndexes
    {
        ThresholdExceeded,
    }

    public static class SensorValuePosterMessages
    {
        public static Dictionary<SensorValuePosterMessagesIndexes, string> Messages =
            new Dictionary<SensorValuePosterMessagesIndexes, string>()
            {
                {SensorValuePosterMessagesIndexes.ThresholdExceeded, "Treshold value exceeded"}
            };
    }
}
