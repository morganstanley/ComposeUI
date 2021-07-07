# Architecture Decision Record: Observability

## Context

Observability is the ability to infer internal states of a system based on the system's external outputs.
Observability is achieved through collection, correlation and interpretation of the three data domains which are also referred to as three pillars of observability: metrics, tracing and logs.
ComposeUI needs to collect and emit these data to provide the ability for users
 - to have access to more accurate information about the system
 - to make monitoring and troubleshooting more efficient
 - to measure their service's behaviour and to evaluate whether their system has been running within SLO (Service Level Objective) 

 ## Decision

 For observability we plan to use the opensource OpenTelemetry observability framework. It is available in all the main programming languages. It offers a single set of APIs and libraries that standardize how to collect and transfer telemetry data. It is vendor neutral so users can send telemetry data to distinct backends of their choice. It improves observability by bringign together traces, logs and metrics from across applications and services in a correlated manner.

 - **Metrics**  
 For metrics the prometheus .net package is going to be used as long as the OpenTelemetry metrics library reaches GA.

 - **Tracing**  
 ComposeUI is going to be instrumented using *System.Diagnostics.Activity* class. This way, users can collect distributed trace information using OpenTelemetry.

 - **Logs**  
 ComposeUI logs are going to be enriched with trace context in order to correlate traces with logs.

 ## Status

 Proposed

 ## Consequences

 - OpenTelemetry provides a unified standard for creating and ingesting telemetry data.
 - It is able to provide developers and site reliability engineers with a more complete impression of app performance than was previously provided by basic monitoring.
 - It helps achieve business objectives.
 - Enables developers, security analysts and product managers to better understand and fix problems in the app that could have a negative impact on the business.