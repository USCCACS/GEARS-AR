using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

public static Vision instance;

// Replace <Subscription Key> with your valid subscription key.
private string subscriptionKey = "<Subscription Key>";

// You must use the same Azure region in your REST API method as you used to
// get your subscription keys. For example, if you got your subscription keys
// from the West US region, replace "westcentralus" in the URL
// below with "westus".
//
// Free trial subscription keys are generated in the "westus" region.
// If you use a free trial subscription key, you shouldn't need to change
// this region.
const string uriBase =
    "https://westus2.api.cognitive.microsoft.com/vision/v2.0/read/core/asyncBatchAnalyze";

internal byte[] imageBytes;

internal string imagePath;

private void Awake()
{
    instance = this;
}

public IEnumerator AnalyseLastImageCaptured()
{
    try
    {
        HttpClient client = new HttpClient();

        // Request headers.
        client.DefaultRequestHeaders.Add(
            "Ocp-Apim-Subscription-Key", subscriptionKey);

        // Assemble the URI for the REST API method.
        string uri = uriBase;

        HttpResponseMessage response;

        // Two REST API methods are required to extract handwritten text.
        // One method to submit the image for processing, the other method
        // to retrieve the text found in the image.

        // operationLocation stores the URI of the second REST API method,
        // returned by the first REST API method.
        string operationLocation;

        // Reads the contents of the specified local image
        // into a byte array.
        byte[] byteData = GetImageAsByteArray(imagePath);

        // Adds the byte array as an octet stream to the request body.
        using (ByteArrayContent content = new ByteArrayContent(byteData))
        {
            // This example uses the "application/octet-stream" content type.
            // The other content types you can use are "application/json"
            // and "multipart/form-data".
            content.Headers.ContentType =
                new MediaTypeHeaderValue("application/octet-stream");

            // The first REST API method, Batch Read, starts
            // the async process to analyze the written text in the image.
            response = await client.PostAsync(uri, content);
        }

        // The response header for the Batch Read method contains the URI
        // of the second method, Read Operation Result, which
        // returns the results of the process in the response body.
        // The Batch Read operation does not return anything in the response body.
        if (response.IsSuccessStatusCode)
            operationLocation =
                response.Headers.GetValues("Operation-Location").FirstOrDefault();
        else
        {
            // Display the JSON error data.
            string errorString = await response.Content.ReadAsStringAsync();
            Console.WriteLine("\n\nResponse:\n{0}\n",
                JToken.Parse(errorString).ToString());
            return;
        }

        // If the first REST API method completes successfully, the second 
        // REST API method retrieves the text written in the image.
        //
        // Note: The response may not be immediately available. Handwriting
        // recognition is an asynchronous operation that can take a variable
        // amount of time depending on the length of the handwritten text.
        // You may need to wait or retry this operation.
        //
        // This example checks once per second for ten seconds.
        string contentString;
        int i = 0;
        do
        {
            System.Threading.Thread.Sleep(1000);
            response = await client.GetAsync(operationLocation);
            contentString = await response.Content.ReadAsStringAsync();
            ++i;
        }
        while (i < 10 && contentString.IndexOf("\"status\":\"Succeeded\"") == -1);

        if (i == 10 && contentString.IndexOf("\"status\":\"Succeeded\"") == -1)
        {
            Console.WriteLine("\nTimeout error.\n");
            return;
        }
        else
        {
            // Display the JSON response.
            Console.WriteLine("\nResponse:\n\n{0}\n",
                JToken.Parse(contentString).ToString());

            List<string> textList = new List<string>();

            // Parse Json to get a list of text lines
            JObject results = new JObject.Parse(contentString);
            // JArray resultLines = (JArray)results["recognitionResults"]["lines"];


            foreach (var ld in results["recognitionResults"]["lines"])
            {
                textList.Add((string)var["text"]);
            }

            ResultsLabel.instance.SetTextToLastLabel(textList);
        }

    }
    catch (Exception e)
    {
        Console.WriteLine("\n" + e.Message);
    }

    yield return null;
}

/// <summary>
/// Returns the contents of the specified file as a byte array.
/// </summary>
private static byte[] GetImageAsByteArray(string imagePath)
{
    FileStream fileStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read);
    BinaryReader BinaryReader = new BinaryReader(fileStream);
    return binaryReader.ReadBytes((int)fileStream.Length);
}