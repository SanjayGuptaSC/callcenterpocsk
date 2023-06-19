# Azure OpenAI Call Center POC

[![Nuget package](https://img.shields.io/nuget/vpre/Microsoft.SemanticKernel)](https://www.nuget.org/packages/Microsoft.SemanticKernel/)
[![dotnet](https://github.com/microsoft/semantic-kernel/actions/workflows/dotnet-ci.yml/badge.svg?branch=main)](https://github.com/microsoft/semantic-kernel/actions/workflows/dotnet-ci.yml)
[![License: MIT](https://img.shields.io/github/license/microsoft/semantic-kernel)](https://github.com/microsoft/semantic-kernel/blob/main/LICENSE)
[![Discord](https://img.shields.io/discord/1063152441819942922?label=Discord&logo=discord&logoColor=white&color=d82679)](https://aka.ms/SKDiscord)

This end-to-end POC of **Azure OpenAI** leverages several Azure services and frameworks to implement a fairly simple Proof on Concept: a Call Center Analysis and Summary application.

# High-level Overview of Program Function
What the program doing high-level is it takes an audio file of a call center call, creates a summary of the conversation using 5 key questions:

1. Main reason of the conversation 
2. Sentiment of the customer 
3. How did the agent handle the conversation? 
4. What was the final outcome of the conversation 
5. Create a short summary of the conversation

It then creates a second summary as an HTML-formatted email that it sends out.

How this is done is via calls to a few **Azure** services.

First it converts the provided audio file to convert it to text via a call to the azure **Speech** cognitive service and it speech to text capabilities.

This text is the basis of the prompts that we then use. 

We then call the **Azure OpenAI** service via a lightwight framework SDK called **Semantic Kernel** to interface with **Azure OpenAI**.

**Semantic Kernel (SK)** is a lightweight SDK enabling integration of AI Large
Language Models (LLMs) with conventional programming languages. The SK extensible
programming model combines natural language **semantic functions**, traditional
code **native functions**, and **embeddings-based memory** unlocking new potential
and adding value to applications with AI. [prompt templating](docs/PROMPT_TEMPLATE_LANGUAGE.md), function chaining, [vectorized memory](docs/EMBEDDINGS.md), and [intelligent planning](docs/PLANNERS.md) capabilities out of the box.

For more go to [plugins](https://learn.microsoft.com/semantic-kernel/howto/).

We will be using it for **Prompts** and for **Summarization**.

The program uses Semantic Kernel to get a summary using the text from the call combined with the 5 key questions.  It outputs this summary to the Console - this could easily be elaborated to write it to a database like CosmosDB to keep a summary resposity of calls with parameters etc. rather than storing full audio files with little or no context.

We then formulate an **HTML** email using a second prompt and call to Semantic Kernel that will be sent to a supervisor email address that is specified in the properties summarizing the call.

The email is sent using the **Azure Email Communication Service** but any email could be configured to send the mail.

## License

Copyright (c) Microsoft Corporation. All rights reserved.

Licensed under the [MIT](LICENSE) license.
