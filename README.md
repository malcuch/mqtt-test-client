# mqtt-test-client

Console app for testing AWS IoT Hub connectivity via MQTT TCP protocol

# What is required on AWS IoT?
1. Create device certificate with private key to authenticate in AWS IoT 
  
  > https://docs.aws.amazon.com/iot/latest/developerguide/device-certs-create.html

2. Policy and thing must be attached to certificate
  
  > https://docs.aws.amazon.com/iot/latest/developerguide/attach-to-cert.html

Sample policy that allows connection from any Client ID, publish, receive messages on any topic.
Note that Resource must be adjusted to actual region and account.
```
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "iot:Publish",
        "iot:Receive",
        "iot:RetainPublish"
      ],
      "Resource": [
        "arn:aws:iot:us-west-2:<AWS-account-ID>:topic/*"
      ]
    },
    {
      "Effect": "Allow",
      "Action": [
        "iot:Subscribe"
      ],
      "Resource": [
        "arn:aws:iot:us-west-2:<AWS-account-ID>:topicfilter/*"
      ]
    },
    {
      "Effect": "Allow",
      "Action": [
        "iot:Connect"
      ],
      "Resource": [
        "arn:aws:iot:us-west-2:<AWS-account-ID>:client/${iot:ClientId}"
      ]
    }
  ]
}
```
