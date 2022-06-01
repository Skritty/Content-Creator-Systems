using UnityEngine;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;
using System.IO;
using System.Collections.Generic;
using Amazon.CognitoIdentity;
using Amazon;
using System;



public class S3Manage : MonoBehaviour
{
    #region VARIABLES

    public static S3Manage Instance { get; set; }

    [Header("AWS Setup")]
    [SerializeField] private string identityPoolId;
    [SerializeField] private string cognitoIdentityRegion = RegionEndpoint.USEast1.SystemName;
    [SerializeField] private string s3Region = RegionEndpoint.USEast1.SystemName;

    // Variables privates
    private int timeoutGetObject = 5; // seconds
    private string resultTimeout = "";
    public Action<GetObjectResponse, string> OnResultGetObject;
    private IAmazonS3 s3Client;
    private AWSCredentials credentials;


    // Propertys
    private RegionEndpoint CognitoIdentityRegion
    {
        get { return RegionEndpoint.GetBySystemName(cognitoIdentityRegion); }
    }
    private RegionEndpoint S3Region
    {
        get { return RegionEndpoint.GetBySystemName(s3Region); }
    }
    private AWSCredentials Credentials;

    private IAmazonS3 Client;


    #endregion

    #region METHODS MONOBEHAVIOUR

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {

        UnityInitializer.AttachToGameObject(this.gameObject);
        AWSConfigs.HttpClient = AWSConfigs.HttpClientOption.UnityWebRequest;
    }


    #endregion

    #region METHODS AWS SDK S3

    /// <summary>
    /// Get Objects from S3 Bucket
    /// </summary>
    public void ListObjectsBucket(string nameBucket, Action<ListObjectsResponse, string> result)
    {
        var request = new ListObjectsRequest()
        {
            BucketName = nameBucket
        };

        Client.ListObjectsAsync(request, (responseObject) =>
        {
            if (responseObject.Exception == null)
                result?.Invoke(responseObject.Response, "");
            else
                result?.Invoke(null, responseObject.Exception.ToString());
        });
    }

    /// <summary>
    /// Get Object from S3 Bucket
    /// </summary>
    public void GetObjectBucket(string S3BucketName, string fileNameOnBucket, Action<GetObjectResponse, string> result)
    {
        resultTimeout = "";
        Invoke("ResultTimeoutGetObjectBucket", timeoutGetObject);

        var request = new GetObjectRequest
        {
            BucketName = S3BucketName,
            Key = fileNameOnBucket
        };

        Client.GetObjectAsync(request, (responseObj) =>
        {
            var response = responseObj.Response;

            if (response.ResponseStream != null)
            {
                result?.Invoke(responseObj.Response, "");
                resultTimeout = "success";
            }
            else
                result?.Invoke(null, responseObj.Exception.ToString());
        });
    }

    /// <summary>
    /// Post Object to S3 Bucket. 
    /// </summary>
    public void UploadObjectForBucket(string pathFile, string S3BucketName, string fileNameOnBucket, Action<PostObjectResponse, string> result)
    {
        if (!File.Exists(pathFile))
        {
            result?.Invoke(null, "FileNotFoundException: Could not find file " + pathFile);
            return;
        }
        credentials = new CognitoAWSCredentials(identityPoolId, CognitoIdentityRegion);
        Client = new AmazonS3Client(credentials, S3Region);


        var stream = new FileStream(pathFile, FileMode.Open, FileAccess.Read, FileShare.Read);
        Debug.Log("Creating request object");
        var request = new PostObjectRequest()
        {
            Bucket = S3BucketName,
            Key = fileNameOnBucket,
            InputStream = stream,
            CannedACL = S3CannedACL.PublicReadWrite,
            Region = S3Region
        };
        Debug.Log("Posting");
        Client.PostObjectAsync(request, (responseObj) =>
        {
            if (responseObj.Exception == null)
                result?.Invoke(responseObj.Response, "");
            else
                result?.Invoke(null, responseObj.Exception.ToString());
        });
    }

    /// <summary>
    /// Delete Objects in S3 Bucket
    /// </summary>
    public void DeleteObjectOnBucket(string fileNameOnBucket, string S3BucketName, Action<DeleteObjectsResponse, string> result)
    {
        List<KeyVersion> objects = new List<KeyVersion>();
        objects.Add(new KeyVersion()
        {
            Key = fileNameOnBucket
        });

        var request = new DeleteObjectsRequest()
        {
            BucketName = S3BucketName,
            Objects = objects
        };

        Client.DeleteObjectsAsync(request, (responseObj) =>
        {
            if (responseObj.Exception == null)
                result?.Invoke(responseObj.Response, "");
            else
                result?.Invoke(null, responseObj.Exception.ToString());
        });
    }

    #endregion

    #region METHODS UTILS

    private void ResultTimeoutGetObjectBucket()
    {
        if (string.IsNullOrEmpty(resultTimeout))
        {
            OnResultGetObject?.Invoke(null, "Timeout GetObject");
        }
    }

    #endregion
}

