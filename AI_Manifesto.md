TrackFI - AI-Driven Development (AIDD) Manifesto
================================================

1. Overview
-----------

### 1.1. Document Purpose

This document defines the principles and practices for human-AI collaboration in the development of TrackFI. It establishes the roles, responsibilities, and workflows that govern our use of AI, ensuring it acts as a force multiplier for productivity, quality, and speed. This is a living document that will evolve as our tools and techniques mature.

### 1.2. Philosophy

We adopt the principles of AI-Driven Development (AIDD). In this paradigm, the AI is the primary **Coder/Doer**, and the human developer is the **Architect/Director**. Our goal is to leverage AI to handle the tactical, line-by-line implementation, freeing up the developer to focus on strategic thinking, architecture, user experience, and creative problem-solving.

2. Guiding Principles
---------------------

1.  **Architecture First:** The AI's effectiveness is directly proportional to the clarity of its instructions. The [**Software Architecture Specification**](/Product-Architecture-Specification) is the single source of truth. All prompts and AI-generated code must strictly adhere to it.
    
2.  **AI is the Coder, Human is the Director:** The AI's role is to write code, generate tests, and create documentation. The developer's role is to define _what_ to build, provide clear direction, review the output, and apply the "taste" and "vision" that ensures a high-quality product.
    
3.  **Iterate, Never Accept Blindly:** The first output from the AI is a draft, not a final product. The core of AIDD is a tight feedback loop of **prompt -> generate -> critique -> refine**. The developer is ultimately responsible for the quality and correctness of every line of code that is committed.
    
4.  **Prompt Engineering is a Core Skill:** The quality of the AI's output is a direct reflection of the quality of the input. We will continuously invest in mastering the skill of writing clear, context-rich, and unambiguous prompts.
    
5.  **Leverage AI Across the Full Stack:** The AI is not just for generating controller actions. Its role extends to creating domain entities, writing infrastructure clients, generating unit tests, writing documentation, and even researching technical solutions.
    

3. Roles & Responsibilities
---------------------------

### 3.1. The AI's Role (The "Doer")

The AI is responsible for the "how" of implementation.
*   **Code Generation:** Implementing features (classes, methods, logic) based on prompts that are grounded in the project's architecture.
    
*   **Boilerplate Elimination:** Creating new files, CQRS handlers, DTOs, validators, and other repetitive structures that conform to our established patterns.
    
*   **Unit Test Generation:** Creating comprehensive unit tests using xUnit and Moq for the Domain and Application layers.
    
*   **Refactoring:** Performing targeted code refactoring based on developer instructions (e.g., "Extract this logic into a separate service").
    
*   **Documentation:** Generating KDoc comments, updating Markdown files, and drafting new wiki pages.
    
*   **Technical Research:** Answering technical questions and providing code examples for new libraries or APIs.
    

### 3.2. The Developer's Role (The "Director")

The developer is responsible for the "what" and the "why," serving as the strategic lead.
*   **Chief Architect:** Owning, maintaining, and updating the `Software Architecture Specification`.
    
*   **Creative Director:** Defining the product vision, user stories, and feature requirements. Applying "taste" to ensure the final output meets quality standards.
    
*   **Prompt Engineer:** Translating feature requirements into precise, effective prompts for the AI.
    
*   **Quality Assurance Lead:** Meticulously reviewing, testing, and validating all AI-generated code. The developer is **100% accountable** for the final codebase.
    
*   **Systems Integrator:** Committing code, managing the CI/CD pipeline, and ensuring the application as a whole works correctly.
    

4. The Core AIDD Workflow Loop
------------------------------

Every task, from a simple bug fix to a new feature, will follow this iterative loop:
1.  **Goal Definition (Developer):** Clearly define the task or user story. What is the desired outcome?
    
2.  **Context Loading (Developer):** Provide the AI with the necessary context. This always includes the `Software Architecture Specification` and any relevant existing files (e.g., the entity, the interface, an existing controller).
    
3.  **Prompt (Developer):** Write a clear, specific, and architecture-aware prompt.
    
4.  **Generation (AI):** The AI generates the initial draft of the code, test, or document.
    
5.  **Critique & Review (Developer):**
    *   **Correctness:** Does the code work? Does it compile? Does it solve the problem?
        
    *   **Architectural Compliance:** Does it follow the patterns from the spec (e.g., CQRS, Clean Architecture layers)?
        
    *   **Cleanliness:** Is the code readable, maintainable, and well-structured?
        
6.  **Refinement (The Loop):** Provide feedback to the AI in follow-up prompts. ("That's a good start, but can you refactor it to use the `IPriceProvider` interface instead of a hardcoded value?"). Continue this loop until the output is satisfactory.
    
7.  **Integration & Verification (Developer):** Manually integrate the final code into the codebase. Run the application and all relevant tests to ensure everything works as expected. Commit the code.
    

5. Tooling
----------

*   **Primary AI Tool:** [Claude Code CLI](https://www.google.com/search?q=https://docs.anthropic.com/en/claude-on-the-command-line "null")
    
*   **Workflow:** The CLI will be used directly in the terminal, within the project's directory structure.
    
*   **Context Management:** We will leverage the CLI's ability to reference files directly (`claude code -a path/to/file.cs`) to provide rich, in-project context. The architecture document will be a mandatory reference for most code-generation tasks.
    

6. Prompting Best Practices for TrackFI
---------------------------------------

*   **Always Reference the Source of Truth:** Start prompts with "Based on the attached `Software Architecture Specification`...".
    
*   **Be Specific and Unambiguous:**
    *   **Bad:** "Make an endpoint to get portfolio data."
        
    *   **Good:** "In `TrackFI.Api`, create a new `PortfolioController`. Inside, create a GET endpoint at `/api/v1/portfolio`. This endpoint should accept `evmAddress` and `solAddress` as optional query parameters. It must create a `GetPortfolioQuery` and send it using the injected MediatR `ISender`. Return the result wrapped in a `200 OK` response."
        
*   **Provide Examples (Few-Shot Prompting):** When asking for a new service or handler, provide an existing, well-structured example from the codebase for the AI to emulate.
    
*   **Assign a Persona:** "You are an expert .NET developer specializing in Clean Architecture and .NET Aspire..."
    
*   **Iterate on Prompts:** If an output isn't right, don't just discard it. Analyze why it failed and refine the prompt to be more specific.
    

7. Limitations & Guardrails
---------------------------

To ensure quality and security, the following rules are non-negotiable:
*   **No Blind Commits:** ALL AI-generated code must be read, understood, and manually verified by the developer before it is committed to the repository. **NO EXCEPTIONS.**
    
*   **Security is a Human Responsibility:** The AI is a tool, not a security expert. All code related to authentication, authorization, data validation, and secrets management requires extreme scrutiny. Do not delegate security decisions to the AI.
    
*   **Verify External Information:** Be aware that the AI can "hallucinate" and invent API endpoints, library features, or configuration values. Always verify information against official documentation.
    
*   **The Developer is Always Accountable:** The AI is a powerful pair programmer, but the developer is the pilot in command. The ultimate responsibility for the project's success and the codebase's integrity rests with the human developer.