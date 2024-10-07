# Equation Solver

This project is an AI-powered equation solver that uses image recognition to extract mathematical equations and solve them. It leverages AutoGen, OpenAI's GPT-4 model, and the StepWise framework to create a workflow for processing and solving equations from images.

## Features

- Image input for equations
- OpenAI API key validation
- Equation extraction from images using GPT-4 vision
- Latex format conversion of extracted equations
- Equation solving using GPT-4

## Prerequisites

- .NET Core SDK 8.0 or later
- OpenAI API key

## Setup

1. Clone the repository
2. Set up your OpenAI API key:
   - Option 1: Set an environment variable named `OPENAI_API_KEY` with your API key
   - Option 2: You'll be prompted to enter the API key when running the application

## Running the Application

1. Build the project:
   ```
   dotnet build
   ```
2. Run the application:
   ```
   dotnet run
   ```
3. The application will start a web server at `http://localhost:5123`

## Usage

1. The application will prompt you to provide an image of an equation
2. If the OpenAI API key is not set as an environment variable, you'll be asked to provide it
3. The system will validate the image to ensure it contains exactly one equation
4. The equation will be extracted from the image and converted to Latex format
5. Finally, the equation will be solved, and the solution will be presented