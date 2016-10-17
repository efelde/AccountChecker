using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;


using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.SimpleDB;
using Amazon.SimpleDB.Model;
using Amazon.S3;
using Amazon.S3.Model;


namespace AccountChecker
{
    class Program
    {
        public static void Main(string[] args)
        {

            Console.WriteLine("===========================================");
            Console.WriteLine("Welcome to the AWS .NET SDK!");
            Console.WriteLine("===========================================");
            String[] creds = GetSSOInfo();

            if (creds != null)
            {
                foreach (RegionEndpoint rEndpoint in RegionEndpoint.EnumerableAllRegions)
                {
                    if (!rEndpoint.DisplayName.Contains("Gov") && !rEndpoint.DisplayName.Contains("China"))
                    {
                        Console.WriteLine(rEndpoint.ToString());
                        Console.WriteLine("======================================================");
                        Console.Write(GetEC2InstancesOutput(rEndpoint));
                        Console.Write(GetSimpleDBDomainsOutput(rEndpoint));
                        Console.WriteLine("======================================================");

                    }
                }

                Console.Write(GetS3BucketsOutput());
                Console.WriteLine();
                Console.WriteLine("Press X key to exit");

                //Console.ReadLine();
            }
            else
            {
                creds = GetSSOInfo();
            }
        }

        public static bool RsaChecker(string token)
        {
            if (token.Length != 10)
                return false;
            else if (float.Parse(token) > 0)
                return true;
            else
                return false;
        }
        public static string GetSSO()
        {
            string sso = "";
            Console.WriteLine("SSO Username:");
            sso = Console.ReadLine().TrimEnd();
            if (sso != "")
                return sso;
            else
                return GetSSO();

        }
        public static bool SsoChecker(string sso)
        {
            if (sso.Length == 8)
                return true;
            else
            {
                Console.WriteLine("======================================================");
                Console.WriteLine("Not a valid SSO");
                return false;
            }
        }

        public static string[] GetSSOInfo()
        {
            StringBuilder sb = new StringBuilder();
            int i, count = 0;
            bool check = false;
            string sso = "";

            do
            {
                sso = GetSSO();
            } while (!SsoChecker(sso));


            Console.WriteLine("RSA Token");


            while ((i = Console.Read()) != 13)
            {
                if (++count > 10) break;
                sb.Append((char)i);
            }
            check = RsaChecker(sb.ToString());
            if (check)
            {
                String[] creds = new String[] { sso, sb.ToString() };
                return creds;
            }
            else
            {
                return GetSSOInfo();
            }
        }

        public static string GetEC2InstancesOutput(RegionEndpoint rEndpoint)
        {
            StringBuilder sb = new StringBuilder(1024);
            using (StringWriter sr = new StringWriter(sb))
            {
                IAmazonEC2 ec2 = new AmazonEC2Client();
                DescribeInstancesRequest ec2Request = new DescribeInstancesRequest();
                Amazon.Runtime.AWSCredentials credentials = new Amazon.Runtime.StoredProfileAWSCredentials("3rdPlayground");
                ec2 = new AmazonEC2Client(credentials, rEndpoint);

                try
                {
                    DescribeInstancesResponse ec2Response = ec2.DescribeInstances(ec2Request);
                    int numInstances = 0;
                    numInstances = ec2Response.Reservations.Count;
                    sr.WriteLine(string.Format("You have {0} Amazon EC2 instance(s) running in the {1} region.",
                    numInstances, rEndpoint.ToString()));

                    DescribeInstancesResponse ec2IdResponse = ec2.DescribeInstances(ec2Request);
                    Amazon.EC2.Model.Reservation[] instanceForGettingIds = ec2IdResponse.Reservations.ToArray();
                    string[] ec2IDs = ec2Request.InstanceIds.ToArray();

                    foreach (Reservation comp in instanceForGettingIds)
                    {
                        sr.WriteLine();
                        sr.WriteLine("--- " + comp.Instances[0].InstanceId);
                    }
                }
                catch (AmazonEC2Exception ex)
                {
                    if (ex.ErrorCode != null && ex.ErrorCode.Equals("AuthFailure"))
                    {
                        sr.WriteLine("The account you are using is not signed up for Amazon EC2.");
                        sr.WriteLine("You can sign up for Amazon EC2 at http://aws.amazon.com/ec2");
                    }
                    else
                    {
                        sr.WriteLine("Caught Exception: " + ex.Message);
                        sr.WriteLine("Response Status Code: " + ex.StatusCode);
                        sr.WriteLine("Error Code: " + ex.ErrorCode);
                        sr.WriteLine("Error Type: " + ex.ErrorType);
                        sr.WriteLine("Request ID: " + ex.RequestId);
                    }
                }

                sr.WriteLine();

            }
            return sb.ToString();
        }

        public static string GetS3BucketsOutput()
        {
            StringBuilder sb = new StringBuilder(1024);
            using (StringWriter sr = new StringWriter(sb))
            {
                IAmazonS3 s3Client = new AmazonS3Client();

                try
                {
                    ListBucketsResponse response = s3Client.ListBuckets();
                    int numBuckets = 0;
                    if (response.Buckets != null &&
                        response.Buckets.Count > 0)
                    {
                        numBuckets = response.Buckets.Count;
                    }
                    sr.WriteLine("You have " + numBuckets + " Amazon S3 bucket(s).");
                    sr.WriteLine();
                    sr.WriteLine();
                    S3Bucket[] buckets = response.Buckets.ToArray();
                    foreach (S3Bucket bucket in buckets)
                    {
                        sr.WriteLine("--- " + bucket.BucketName.PadRight(63) + " --- " + bucket.CreationDate);
                    }

                }
                catch (AmazonS3Exception ex)
                {
                    if (ex.ErrorCode != null && (ex.ErrorCode.Equals("InvalidAccessKeyId") ||
                        ex.ErrorCode.Equals("InvalidSecurity")))
                    {
                        sr.WriteLine("Please check the provided AWS Credentials.");
                        sr.WriteLine("If you haven't signed up for Amazon S3, please visit http://aws.amazon.com/s3");
                    }
                    else
                    {
                        sr.WriteLine("Caught Exception: " + ex.Message);
                        sr.WriteLine("Response Status Code: " + ex.StatusCode);
                        sr.WriteLine("Error Code: " + ex.ErrorCode);
                        sr.WriteLine("Request ID: " + ex.RequestId);
                    }
                }
            }
            return sb.ToString();
        }

        public static string GetSimpleDBDomainsOutput(RegionEndpoint rEndpoint)
        {
            StringBuilder sb = new StringBuilder(1024);
            using (StringWriter sr = new StringWriter(sb))
            {
                IAmazonSimpleDB simpleDBClient = new AmazonSimpleDBClient();

                try
                {
                    ListDomainsResponse response = simpleDBClient.ListDomains();
                    int numDomains = 0;
                    if (response.DomainNames != null &&
                        response.DomainNames.Count > 0)
                    {
                        numDomains = response.DomainNames.Count;
                    }
                    sr.WriteLine(string.Format("You have {0} Amazon SimpleDB domain(s) in the {1} region.",
                                               numDomains, rEndpoint.ToString())); 
                    sr.WriteLine();
                    sr.WriteLine();
                    //S3Bucket[] domains = response.DomainNames.ToArray();
                    //foreach ( bucket in buckets)
                    //{
                    //    sr.WriteLine("--- " + bucket.BucketName.PadRight(63) + " --- " + bucket.CreationDate);
                    //}

                }
                catch (AmazonS3Exception ex)
                {
                    if (ex.ErrorCode != null && (ex.ErrorCode.Equals("InvalidAccessKeyId") ||
                        ex.ErrorCode.Equals("InvalidSecurity")))
                    {
                        sr.WriteLine("Please check the provided AWS Credentials.");
                        sr.WriteLine("If you haven't signed up for Amazon S3, please visit http://aws.amazon.com/s3");
                    }
                    else
                    {
                        sr.WriteLine("Caught Exception: " + ex.Message);
                        sr.WriteLine("Response Status Code: " + ex.StatusCode);
                        sr.WriteLine("Error Code: " + ex.ErrorCode);
                        sr.WriteLine("Request ID: " + ex.RequestId);
                    }
                }
            }

            return sb.ToString();
        }
        
    }
}