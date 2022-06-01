using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using LightBuzz.Archiver;
using Amazon.S3.Model;
using UnityEngine.UI;
public class S3Upload : MonoBehaviour
{
    public ProjectManager projManager;
    public S3Manage manager;
    private const string awsBucketName = "aireal-realitystudio";
    private const string awsAccessKey = "AKIASJE2LZECD26ZMS7M";
    private const string awsSecretKey = "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";
    
    [SerializeField] 
    private Text resultTextOperation;

    public void UploadStart()
    {
        if(projManager.projectDirectory.FullName != null)
        {
            string destination = Application.persistentDataPath + Path.GetFileNameWithoutExtension(projManager.projectDirectory.FullName) + ".zip";

            Archiver.Compress(projManager.projectDirectory.FullName, destination);
            UploadObjectForBucket(destination, "aireal-realitystudio", "CC/" + Path.GetFileName(projManager.projectDirectory.FullName));
        }

    }


    private void UploadObjectForBucket(string pathFile, string S3BucketName, string fileNameOnBucket)
    {
        resultTextOperation.text = "Uploading Project for processing...";


        manager.UploadObjectForBucket(pathFile, S3BucketName, fileNameOnBucket, (result, error) =>
        {
            if (string.IsNullOrEmpty(error))
            {
                resultTextOperation.text = "Upload Success";
            }
            else
            {
                resultTextOperation.text = "Upload Failed";
                Debug.LogError("Get Error:: " + error);
            }
        });
    }
}

