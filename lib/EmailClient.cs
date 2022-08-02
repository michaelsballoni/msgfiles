﻿using Amazon;
using Amazon.Runtime;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;

namespace msgfiles
{
    public class EmailClient
    {
        public EmailClient(string accessKey, string secretKey, string region)
        {
            m_client = 
                new AmazonSimpleEmailServiceV2Client
                (
                    new BasicAWSCredentials(accessKey, secretKey), 
                    RegionEndpoint.GetBySystemName(region)
                );
        }

        public async Task 
            SendEmailAsync
            (
                string from, // display <email> or just email
                Dictionary<string, string> toAddrs, // email -> display
                string subject, 
                string body
            )
        {
            var msg = new SendEmailRequest();

            msg.FromEmailAddress = from;

            msg.Destination = new Destination();
            msg.Destination.ToAddresses = 
                new List<string>
                (
                    toAddrs.Select
                    (
                        kvp => kvp.Value.Length > 0 ? $"{kvp.Value} <{kvp.Key}>" : kvp.Key
                    )
                );

            msg.Content =
                new EmailContent()
                {
                    Simple = new Message()
                    {
                        Subject = new Content() { Charset = "UTF-8", Data = subject },
                        Body = new Body() { Text = new Content() { Charset = "UTF-8", Data = body } }
                    }
                };

            await m_client.SendEmailAsync(msg);
        }

        private AmazonSimpleEmailServiceV2Client m_client;
    }
}
