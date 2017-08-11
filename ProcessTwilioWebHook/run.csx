using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using Twilio.TwiML;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, IAsyncCollector<string> outputGuestbookItem, TraceWriter log)
{
    log.Info("Webhook triggered from Twilio");

    // gather values from SMS webhook
    var data = await req.Content.ReadAsStringAsync();
    var formValues = data.Split('&')
        .Select(value => value.Split('='))
        .ToDictionary(pair => Uri.UnescapeDataString(pair[0]).Replace("+", " "), 
                      pair => Uri.UnescapeDataString(pair[1]).Replace("+", " "));
    var smsFrom = formValues["From"];
    var smsBody = formValues["Body"];

    // Get sentiment score for post
    var score = await GetSentimentScore(smsBody);

    // Check for profanity
    string cognitiveUri = "https://westus.api.cognitive.microsoft.com/contentmoderator/moderate/v1.0/ProcessText/Screen/?language=eng&autocorrect=true";    
    using (var client = new HttpClient())
    {
        client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", Environment.GetEnvironmentVariable("ContentAPIKey"));
        using (var cogresponse = await client.PostAsync(cognitiveUri, new StringContent(smsBody, Encoding.UTF8, "text/plain")))
        {
            //log.Info(cogresponse.ToString());
            var responseContent = await cogresponse.Content.ReadAsStringAsync();
            //log.Info(responseContent.IndexOf("\"Terms\":null").ToString());
            if(responseContent.IndexOf("\"Terms\":null")!=-1)
            {
                log.Info("No profanity");
            }
            else
            {
                smsBody = "*** MESSAGE REDACTED DUE TO BAD LANGUAGE ***";
            }
        }
    }

    // Build message for Queue
    var queueMessage = smsFrom + "|" + smsBody + "|" + score;

    log.Info("Writing to queue: " + queueMessage);

    // write values to Azure queue
    await outputGuestbookItem.AddAsync(queueMessage);

    // create SMS response to sender
    var response = new MessagingResponse().Message($"Thanks for participating{smsFrom}. Message you sent: {smsBody} (Score={score})");
    var twiml = response.ToString();
    twiml = twiml.Replace("utf-16", "utf-8");

    return new HttpResponseMessage
    {
        Content = new StringContent(twiml, Encoding.UTF8, "application/xml")
    };
}

public async static Task<float> GetSentimentScore(string text)
{
    var document = JsonConvert.SerializeObject(new { documents = new[] { new { language = "en", id = 1, text = text } } });
    string sentimentUri = "https://westus.api.cognitive.microsoft.com/text/analytics/v2.0/sentiment";

    using (var client = new HttpClient())
    {
        client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", Environment.GetEnvironmentVariable("SentimentAPIKey"));
        using (var response = await client.PostAsync(sentimentUri, new StringContent(document, Encoding.UTF8, "application/json")))
        {
            JObject result = await response.Content.ReadAsAsync<JObject>();
            return result.SelectToken("documents[0].score")?.Value<float>() ?? 0;
        }
    }
}