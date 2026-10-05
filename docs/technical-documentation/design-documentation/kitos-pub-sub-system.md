# Kitos pub/sub system

[Original Confluence page](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/1117749250)

# Introduction

This page describes the design of the “PubSub” component

Kitos PubSub was motivated by the DBS system’s need to keep its data up to date with KITOS. The DBS system has multiple overlapping data fields with Kitos, this requires the data to be kept consistent across both systems.

This requirement caused the need for a PubSub component. The intent is to allow external integrations to subscribe to certain Kitos events, allowing them to keep their data up to date. Main PubSub functionalities are:

* **Publish** - allows Kitos to push certain events to Api Users subscribed to the published topic. PubSub calls the callback URL provided with the subscribe request.
* **Subscribe** - allows Api users to subscribe to Kitos events, and await updates on provided callback.

# **Solution overview**

The following diagram gives an overview of the solution. At the moment of writing the only known subscriber is DBS; others will subscribe to queues in the same way.

As mentioned before PubSub has two use cases: **Publish** and **Subscribe**

* **The publish** endpoint takes a Kitos change event and publishes it using RabbitMQ. A background service upon receiving an event from RabbitMQ interprets the event, retrieves the event subscribers, and pushes the event to callbackUrls defined for each subscriber.
  The callback request is secured using a pre-defined API key encoded by the [HMAC method](https://webhooks.fyi/security/hmac).
* **The subscribe** endpoint requires a callback and a topic. These are then subscribed to an event and called back upon being triggered

Both endpoints are being authorized using the [KITOS Token](https://strongminds.atlassian.net/wiki/spaces/KITOS/pages/860946433/Security#KITOS-Token). The token is then verified against the KITOS API. **The publish** endpoint is only allowed to be called by a User with a GlobalAdmin role. **The subscribe** endpoint can only be called by SystemIntegrator API Users.

The solution utilizes [RabbitMQ](https://www.rabbitmq.com/) as its message broker.

## **Queues and message types**

To keep different topics distinct and make the pub/sub system easy to use for subscribers, we expect to implement one queue in RabbitMQ for each message type.

These message types will be included:

* KitosITSystemChangedEvent

Note that both of the immediate requirements from DBS (system name changed, and data processor changed) are covered by the same event. It is then up to clients to compare the changed object to their own existing state to check for changes that they are interested in.
