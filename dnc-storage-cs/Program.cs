//To interact with Amazon S3.
using Amazon;
using Amazon.Runtime.Internal;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Intrinsics.X86;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace storage
{
  class Program
  {
    static int/*async Task*/ Main(/*string[] args*/)
    {
      AmazonS3Client client;
      try
      {
        /***
        string? ENDPOINT = Environment.GetEnvironmentVariable("ENDPOINT");
        if (string.IsNullOrEmpty(ENDPOINT))
        {
          Console.WriteLine("The environment variable ENDPOINT is missing.");
          return;
        }
        string? API_KEY = Environment.GetEnvironmentVariable("API_KEY");
        if (string.IsNullOrEmpty(API_KEY))
        {
          Console.WriteLine("The environment variable API_KEY is missing.");
          return;
        }
        string? SERVICE_INSTANCE_ID = Environment.GetEnvironmentVariable("SERVICE_INSTANCE_ID");
        if (string.IsNullOrEmpty(SERVICE_INSTANCE_ID))
        {
          Console.WriteLine("The environment variable SERVICE_INSTANCE_ID is missing.");
          return;
        }
        string? REGION = Environment.GetEnvironmentVariable("REGION");
        if (string.IsNullOrEmpty(REGION))
        {
          Console.WriteLine("The environment variable REGION is missing.");
          return;
        }
        ***/
        //Initialize configuration.
        AmazonS3Config S3Config = new AmazonS3Config
        {
          //    ServiceURL = ENDPOINT
          //ServiceURL = "https://control.cloud-object-storage.cloud.ibm.com/v2/endpoints"
          ServiceURL = "https://s3.us-east.cloud-object-storage.appdomain.cloud",
          RegionEndpoint = RegionEndpoint.USEast1
        };
        //  client = new AmazonS3Client(API_KEY, SERVICE_INSTANCE_ID, REGION, S3Config);
        client = new AmazonS3Client("b4e1de4e02ca4248a3a305507a648e09", "73ec8154b72677b5d3f707bbc034825a593769756005a09b", S3Config);
        //
        string? PORT = Environment.GetEnvironmentVariable("PORT");
        if (string.IsNullOrEmpty(PORT))
        {
          PORT = "8080";
        }
        //  string connectionUrl = $"http://*:{PORT}/";
        string connectionUrl = "http://127.0.0.1:8001/";
        Console.WriteLine($"Using {connectionUrl}");
        //PS> Invoke-WebRequest -URI http://127.0.0.1:8001/
        //PS> curl.exe localhost:8001
        //PS> curl.exe localhost:8001 -i
        using var listener = new HttpListener();
        listener.Prefixes.Add(connectionUrl);
        listener.Start();
        Console.WriteLine($"Listening on {connectionUrl}");
        while (true)
        {
          //Waiting for an incoming request.
          HttpListenerContext ctx = listener.GetContext();
          HttpListenerRequest req = ctx.Request;
          Console.WriteLine($"Received request: {req.HttpMethod} {req.Url}");
          string? url = req.RawUrl;
          if (url != null)
          {
            url = url.Split('?')[0];
          }
          //
          if (url == null)
          {
            using HttpListenerResponse resp = ctx.Response;
            resp.StatusCode = (int)HttpStatusCode.NotFound;
            resp.StatusDescription = "Not found";
          }
          //http://xxx.xxx.xxx.xxx/storage/blob/ListBuckets
          else if (string.Equals(url, "/storage/blob/ListBuckets", StringComparison.OrdinalIgnoreCase))
          {
            ListBuckets(client, ctx);
          }
          //http://xxx.xxx.xxx.xxx/storage/blob/CreateBucket?bucket=xxxx
          else if (string.Equals(url, "/storage/blob/CreateBucket", StringComparison.OrdinalIgnoreCase))
          {
            CreateBucket(client, ctx);
          }
          //http://xxx.xxx.xxx.xxx/storage/blob/DeleteBucket?bucket=XXXX
          else if (string.Equals(url, "/storage/blob/DeleteBucket", StringComparison.OrdinalIgnoreCase))
          {
            DeleteBucket(client, ctx);
          }
          //http://xxx.xxx.xxx.xxx/storage/blob/ListItemsInBucket?bucket=xxxx
          else if (string.Equals(url, "/storage/blob/ListItemsInBucket", StringComparison.OrdinalIgnoreCase))
          {
            ListItemsInBucket(client, ctx);
          }
          //http://xxx.xxx.xxx.xxx/storage/blob/DeleteItemFromBucket?bucket=xxxx&item=xxx
          else if (string.Equals(url, "/storage/blob/DeleteItemFromBucket", StringComparison.OrdinalIgnoreCase))
          {
            DeleteItemFromBucket(client, ctx);
          }
          //http://XXX.XXX.XXX.XXX/storage/blob/DownloadBlobFile?bucket=xxxx&path=xxxx&item=xxxx
          else if (string.Equals(url, "/storage/blob/DownloadBlobFile", StringComparison.OrdinalIgnoreCase))
          {
            DownloadBlobFile(client, ctx);
          }
          //http://XXX.XXX.XXX.XXX/storage/blob/UploadBlobFile?bucket=xxxx&path=xxx&item=xxx
          else if (string.Equals(url, "/storage/blob/UploadBlobFile", StringComparison.OrdinalIgnoreCase))
          {
            UploadBlobFile(client, ctx);
          }
          else
          {
            NotFound(ctx);
          }
        }
      }
      finally
      {
      }
    }

    static void NotFound(HttpListenerContext ctx)
    {
      try
      {
        using HttpListenerResponse resp = ctx.Response;
        resp.Headers.Set("Content-Type", "text/plain");
        ctx.Response.StatusCode = (int)HttpStatusCode.NotFound;
        string err = "<HTML><BODY>404 - Not Found</BODY></HTML>";
        byte[] buffer = Encoding.UTF8.GetBytes(err);
        resp.ContentLength64 = buffer.Length;
        using Stream respStream = resp.OutputStream;
        respStream.Write(buffer, 0, buffer.Length);
      }
      finally
      {
      }
    }

    static async void ListBuckets(AmazonS3Client client, HttpListenerContext ctx)
    {
      using HttpListenerResponse resp = ctx.Response;
      StringBuilder sb = new StringBuilder(5 * 1024);
      try
      {
        HttpListenerRequest req = ctx.Request;
        var query = req.QueryString;
        if (query == null || query.Count != 0)
        {
          sb.AppendLine("Incorrect number of parameters.");
        }
        else
        {
          var buckets = await client.ListBucketsAsync();
          sb.AppendLine($"Owner: {buckets.Owner.DisplayName}");
          sb.AppendLine($"Number of buckets: {buckets.Buckets.Count}");
          foreach (S3Bucket bucket in buckets.Buckets)
          {
            sb.AppendLine($"Bucket name: {bucket.BucketName}, created on: {bucket.CreationDate}");
          }
        }
      }
      catch (Exception e)
      {
        Console.Write(e.ToString());
        sb.AppendLine(e.Message);
      }
      finally
      {
        resp.Headers.Set("Content-Type", "text/plain");
        var buffer = Encoding.UTF8.GetBytes(sb.ToString());
        resp.ContentLength64 = buffer.Length;
        using Stream ros = resp.OutputStream;
        ros.Write(buffer, 0, buffer.Length);
      }
      return;
    }

    static async void CreateBucket(AmazonS3Client client, HttpListenerContext ctx)
    {
      using HttpListenerResponse resp = ctx.Response;
      StringBuilder sb = new StringBuilder(1 * 1024);
      try
      {
        HttpListenerRequest req = ctx.Request;
        var query = req.QueryString;
        if (query == null || query.Count != 1)
        {
          sb.AppendLine("Incorrect number of parameters.");
        }
        else if (query.Get("bucket") == null)
        {
          sb.AppendLine("Invalid parameter name.");
        }
        else
        {
          var request = new PutBucketRequest
          {
            BucketName = query.Get("bucket"),
            //UseClientRegion = true
          };
          var response = await client.PutBucketAsync(request);
          if (response.HttpStatusCode == HttpStatusCode.OK)
          {
            sb.AppendLine($"Bucket {query.Get("bucket")} created.");
          }
          else
          {
            sb.AppendLine($"Bucket {query.Get("bucket")} was not created.");
          }
        }
      }
      catch (Exception e)
      {
        Console.Write(e.ToString());
        sb.AppendLine(e.Message);
      }
      finally
      {
        resp.Headers.Set("Content-Type", "text/plain");
        var buffer = Encoding.UTF8.GetBytes(sb.ToString());
        resp.ContentLength64 = buffer.Length;
        using Stream ros = resp.OutputStream;
        ros.Write(buffer, 0, buffer.Length);
      }
      return;
    }

    static async void DeleteBucket(AmazonS3Client client, HttpListenerContext ctx)
    {
      using HttpListenerResponse resp = ctx.Response;
      StringBuilder sb = new StringBuilder(1 * 1024);
      try
      {
        HttpListenerRequest req = ctx.Request;
        var query = req.QueryString;
        if (query == null || query.Count != 1)
        {
          sb.AppendLine("Incorrect number of parameters.");
        }
        else if (query.Get("bucket") == null)
        {
          sb.AppendLine("Invalid parameter name.");
        }
        else
        {
          var request = new DeleteBucketRequest
          {
            BucketName = query.Get("bucket")
          };
          var response = await client.DeleteBucketAsync(request);
          if (response.HttpStatusCode == HttpStatusCode.OK)
          {
            sb.AppendLine($"Bucket {query.Get("bucket")} deleted.");
          }
          else
          {
            sb.AppendLine($"Bucket {query.Get("bucket")} was not deleted.");
          }
        }
      }
      catch (Exception e)
      {
        Console.Write(e.ToString());
        sb.AppendLine(e.Message);
      }
      finally
      {
        resp.Headers.Set("Content-Type", "text/plain");
        var buffer = Encoding.UTF8.GetBytes(sb.ToString());
        resp.ContentLength64 = buffer.Length;
        using Stream ros = resp.OutputStream;
        ros.Write(buffer, 0, buffer.Length);
      }
      return;
    }

    static async void ListItemsInBucket(AmazonS3Client client, HttpListenerContext ctx)
    {
      using HttpListenerResponse resp = ctx.Response;
      StringBuilder sb = new StringBuilder(5 * 1024);
      try
      {
        HttpListenerRequest req = ctx.Request;
        var query = req.QueryString;
        if (query == null || query.Count != 1)
        {
          sb.AppendLine("Incorrect number of parameters.");
        }
        else if (query.Get("bucket") == null)
        {
          sb.AppendLine("Invalid parameter name.");
        }
        else
        {
          string? bucket = query.Get("bucket");
          var request = new ListObjectsV2Request
          {
            BucketName = bucket,
            //MaxKeys = 5
          };
          ListObjectsV2Response response;
          Console.WriteLine($"Listing the contents of {bucket}:");
          sb.AppendLine($"Listing the contents of {bucket}:");
          do
          {
            response = await client.ListObjectsV2Async(request);
            response.S3Objects.ForEach(obj => Console.WriteLine($"{obj.Key,-35}{obj.LastModified.ToShortDateString(),10}{obj.Size,10}"));
            response.S3Objects.ForEach(obj => sb.AppendLine($"{obj.Key,-35}{obj.LastModified.ToShortDateString(),10}{obj.Size,10}"));
            //If the response is truncated, set the request ContinuationToken from the NextContinuationToken property of the response.
            request.ContinuationToken = response.NextContinuationToken;
          } while (response.IsTruncated);
        }
      }
      catch (Exception e)
      {
        Console.Write(e.ToString());
        sb.AppendLine(e.Message);
      }
      finally
      {
        resp.Headers.Set("Content-Type", "text/plain");
        var buffer = Encoding.UTF8.GetBytes(sb.ToString());
        resp.ContentLength64 = buffer.Length;
        using Stream ros = resp.OutputStream;
        ros.Write(buffer, 0, buffer.Length);
      }
      return;
    }

    static async void DeleteItemFromBucket(AmazonS3Client client, HttpListenerContext ctx)
    {
      using HttpListenerResponse resp = ctx.Response;
      StringBuilder sb = new StringBuilder(1 * 1024);
      try
      {
        HttpListenerRequest req = ctx.Request;
        var query = req.QueryString;
        if (query == null || query.Count != 2)
        {
          sb.AppendLine("Incorrect number of parameters.");
        }
        else if (query.Get("bucket") == null || query.Get("item") == null)
        {
          sb.AppendLine("Invalid parameter name.");
        }
        else
        {
          string? bucket = query.Get("bucket");
          string? item = query.Get("item");
          var request = new DeleteObjectRequest
          {
            BucketName = bucket,
            Key = item
          };
          Console.WriteLine($"Deleting object: {bucket}:{item}");
          //If the Amazon S3 bucket is located in an AWS Region other than the Region of the default
          //account, define the AWS Region for the Amazon S3 bucket in your call to the AmazonS3Client
          //constructor.
          await client.DeleteObjectAsync(request);
          Console.WriteLine($"Object: {item} deleted from {bucket}.");
          sb.AppendLine($"Object: {item} deleted from {bucket}.");
        }
      }
      catch (Exception e)
      {
        Console.Write(e.ToString());
        sb.AppendLine(e.Message);
      }
      finally
      {
        resp.Headers.Set("Content-Type", "text/plain");
        var buffer = Encoding.UTF8.GetBytes(sb.ToString());
        resp.ContentLength64 = buffer.Length;
        using Stream ros = resp.OutputStream;
        ros.Write(buffer, 0, buffer.Length);
      }
      return;
    }

    static async void DownloadBlobFile(AmazonS3Client client, HttpListenerContext ctx)
    {
      using HttpListenerResponse resp = ctx.Response;
      StringBuilder sb = new StringBuilder(1 * 1024);
      try
      {
        HttpListenerRequest req = ctx.Request;
        var query = req.QueryString;
        if (query == null || query.Count != 3)
        {
          sb.AppendLine("Incorrect number of parameters.");
        }
        else if (query.Get("bucket") == null || query.Get("path") == null || query.Get("item") == null)
        {
          sb.AppendLine("Invalid parameter name.");
        }
        else
        {
          string? bucket = query.Get("bucket");
          string? item = query.Get("item");
          string? path = query.Get("path");
          var request = new GetObjectRequest
          {
            BucketName = bucket,
            Key = item
          };
          Console.WriteLine($"Downloading object: {bucket}:{item}");
          //Issue request and remember to dispose of the response
          using GetObjectResponse response = await client.GetObjectAsync(request);
          try
          {
            //Save object to local file
            await response.WriteResponseStreamToFileAsync($"{path}\\{item}", true, CancellationToken.None);
            Console.WriteLine($"Downloaded object: {path}\\{item}");
            sb.AppendLine($"Downloaded object: {path}\\{item}");
          }
          catch (AmazonS3Exception ex)
          {
            Console.WriteLine($"Error downloading {bucket}:{item}\n{ex.Message}");
            sb.AppendLine($"Error downloading {bucket}:{item}\n{ex.Message}");
          }
        }
      }
      catch (Exception e)
      {
        Console.Write(e.ToString());
        sb.AppendLine(e.Message);
      }
      finally
      {
        resp.Headers.Set("Content-Type", "text/plain");
        var buffer = Encoding.UTF8.GetBytes(sb.ToString());
        resp.ContentLength64 = buffer.Length;
        using Stream ros = resp.OutputStream;
        ros.Write(buffer, 0, buffer.Length);
      }
      return;
    }

    static async void UploadBlobFile(AmazonS3Client client, HttpListenerContext ctx)
    {
      using HttpListenerResponse resp = ctx.Response;
      StringBuilder sb = new StringBuilder(1 * 1024);
      try
      {
        HttpListenerRequest req = ctx.Request;
        var query = req.QueryString;
        if (query == null || query.Count != 3)
        {
          sb.AppendLine("Incorrect number of parameters.");
        }
        else if (query.Get("bucket") == null || query.Get("path") == null || query.Get("item") == null)
        {
          sb.AppendLine("Invalid parameter name.");
        }
        else
        {
          string? bucket = query.Get("bucket");
          string? item = query.Get("item");
          string? path = query.Get("path");
          string filePath = @$"{path}\{item}";
          Stream input = new FileStream(filePath, FileMode.Open, FileAccess.Read);
          var request = new PutObjectRequest
          {
            BucketName = bucket,
            Key = item,
            FilePath = path,
            StorageClass = S3StorageClass.Standard,
            CannedACL = S3CannedACL.NoACL,
            //InputStream = input
          };
          Console.WriteLine($"Uploading object: {bucket}:{item}");
          var response = await client.PutObjectAsync(request);
          if (response.HttpStatusCode == HttpStatusCode.OK)
          {
            Console.WriteLine($"Successfully uploaded {item} to {bucket}.");
            sb.AppendLine($"Successfully uploaded {item} to {bucket}.");
          }
          else
          {
            Console.WriteLine($"Could not upload {item} to {bucket}.");
            sb.AppendLine($"Could not upload {item} to {bucket}.");
          }
        }
      }
      catch (Exception e)
      {
        Console.Write(e.ToString());
        sb.AppendLine(e.Message);
      }
      finally
      {
        resp.Headers.Set("Content-Type", "text/plain");
        var buffer = Encoding.UTF8.GetBytes(sb.ToString());
        resp.ContentLength64 = buffer.Length;
        using Stream ros = resp.OutputStream;
        ros.Write(buffer, 0, buffer.Length);
      }
      return;
    }
  }
}
