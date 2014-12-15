// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// THIS FILE IS NOT INTENDED TO BE EDITED. 
// 
// This file can be updated in-place using the Package Manager Console. To check for updates, run the following command:
// 
// PM> Get-Package -Updates

using System;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

namespace Microsoft.Its.Domain.ServiceBus
{
#if !RecipesProject
    /// <summary>
    /// Provides methods for configuring Service Bus entities.
    /// </summary>
    [System.Diagnostics.DebuggerStepThrough]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
    public static class ServiceBusConfigurer
    {
        /// <summary>
        ///     Creates a queue client, as well as the target queue if it does not already exist.
        /// </summary>
        public static QueueClient CreateQueueClient(
            this ServiceBusSettings settings,
            string queueName,
            Action<QueueDescription> configure = null)
        {
            queueName = queueName.PrefixedIfConfigured(settings);
            var queueDescription = new QueueDescription(queueName);
            if (configure != null)
            {
                configure(queueDescription);
            }
            settings.CreateQueueIfDoesNotAlreadyExist(queueDescription);
            return QueueClient.CreateFromConnectionString(settings.ConnectionString, queueName);
        }

        /// <summary>
        ///     Creates a subscription client, as well as the target subscription if it does not already exist.
        /// </summary>
        public static SubscriptionClient CreateSubscriptionClient(
            this ServiceBusSettings settings,
            string topicPath,
            string subscriptionName,
            Action<SubscriptionDescription> configure = null)
        {
            topicPath = topicPath.PrefixedIfConfigured(settings);
            var subscriptionDescription = new SubscriptionDescription(topicPath, subscriptionName);
            if (configure != null)
            {
                configure(subscriptionDescription);
            }
            settings.CreateSubscriptionIfDoesNotAlreadyExist(subscriptionDescription);
            return SubscriptionClient.CreateFromConnectionString(settings.ConnectionString, subscriptionDescription.TopicPath, subscriptionDescription.Name);
        }

        /// <summary>
        ///     Creates a topic client, as well as the target topic if it does not already exist.
        /// </summary>
        public static TopicClient CreateTopicClient(
            this ServiceBusSettings settings,
            string topicName,
            Action<TopicDescription> configure = null)
        {
            topicName = topicName.PrefixedIfConfigured(settings);
            var topicDescription = new TopicDescription(topicName);
            if (configure != null)
            {
                configure(topicDescription);
            }
            settings.CreateTopicIfDoesNotAlreadyExist(topicDescription);
            return TopicClient.CreateFromConnectionString(settings.ConnectionString, topicName);
        }

        private static void CreateQueueIfDoesNotAlreadyExist(
            this ServiceBusSettings settings,
            QueueDescription queueDescription)
        {
            var namespaceManager = NamespaceManager.CreateFromConnectionString(settings.ConnectionString);

            if (!namespaceManager.QueueExists(queueDescription.Path))
            {
                namespaceManager.CreateQueue(queueDescription);
            }
        }

        private static void CreateSubscriptionIfDoesNotAlreadyExist(
            this ServiceBusSettings settings,
            SubscriptionDescription subscriptionDescription)
        {
            var namespaceManager = NamespaceManager.CreateFromConnectionString(settings.ConnectionString);

            if (!namespaceManager.SubscriptionExists(subscriptionDescription.TopicPath, subscriptionDescription.Name))
            {
                namespaceManager.CreateSubscription(subscriptionDescription);
            }
        }

        private static void CreateTopicIfDoesNotAlreadyExist(
            this ServiceBusSettings settings,
            TopicDescription topicDescription)
        {
            var namespaceManager = NamespaceManager.CreateFromConnectionString(settings.ConnectionString);

            if (!namespaceManager.TopicExists(topicDescription.Path))
            {
                namespaceManager.CreateTopic(topicDescription);
            }
        }

        private static string PrefixedIfConfigured(this string name, ServiceBusSettings settings)
        {
            return (string.IsNullOrEmpty(settings.NamePrefix) ? "" : settings.NamePrefix + "_") + name;
        }
    }
}