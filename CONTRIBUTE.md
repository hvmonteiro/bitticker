# Contributing to BitTicker

First off, thanks for taking the time to contribute! 

All types of contributions are encouraged and valued. See the [Table of Contents](#table-of-contents) for different ways to help and details about how this project handles them. Please make sure to read the relevant section before making your contribution. It will make it a lot easier for the maintainers and smooth out the experience for all involved. The community looks forward to your contributions. 

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [I Have a Question](#i-have-a-question)
- [I Want To Contribute](#i-want-to-contribute)
  - [Reporting Bugs](#reporting-bugs)
  - [Suggesting Enhancements](#suggesting-enhancements)
  - [Your First Code Contribution](#your-first-code-contribution)
  - [Improving The Documentation](#improving-the-documentation)
- [Styleguides](#styleguides)
  - [Commit Messages](#commit-messages)
  - [C# Style Guide](#c-style-guide)

## Code of Conduct

This project and everyone participating in it is governed by the [BitTicker Code of Conduct](CODE_OF_CONDUCT.md). By participating, you are expected to uphold this code.

## I Have a Question

> If you want to ask a question, we assume that you have read the available [Documentation](README.md).

Before you ask a question, it is best to search for existing [Issues](https://github.com/hvmonteiro/BitTicker/issues) that might help you. In case you have found a suitable issue and still need clarification, you can write your question in this issue.

If you then still feel the need to ask a question and need clarification, we recommend the following:

- Open an [Issue](https://github.com/hvmonteiro/BitTicker/issues/new).
- Provide as much context as you can about what you're running into.
- Provide project and platform versions (Windows version, .NET version, etc), depending on what seems relevant.

## I Want To Contribute

> ### Legal Notice
> When contributing to this project, you must agree that you have authored 100% of the content, that you have the necessary rights to the content and that the content you contribute may be provided under the project license.

### Reporting Bugs

#### Before Submitting a Bug Report

A good bug report shouldn't leave others needing to chase you up for more information. Therefore, we ask you to investigate carefully, collect information and describe the issue in detail in your report.

- Make sure that you are using the latest version.
- Determine if your bug is really a bug and not an error on your side (Make sure that you have read the [documentation](README.md)).
- Check if other users have experienced (and potentially already solved) the same issue you are having.
- Collect information about the bug:
  - Stack trace (Traceback)
  - OS, Platform and Version (Windows 10, Windows 11, etc.)
  - Version of .NET runtime
  - CoinMarketCap API status
  - Configuration file contents (remove API key)
  - Can you reliably reproduce the issue?

#### How Do I Submit a Good Bug Report?

We use GitHub issues to track bugs and errors. If you run into an issue with the project:

- Open an [Issue](https://github.com/hvmonteiro/BitTicker/issues/new).
- Explain the behavior you would expect and the actual behavior.
- Please provide as much context as possible and describe the *reproduction steps*.
- Provide the information you collected in the previous section.

### Suggesting Enhancements

#### Before Submitting an Enhancement

- Make sure that you are using the latest version.
- Read the [documentation](README.md) carefully and find out if the functionality is already covered.
- Perform a [search](https://github.com/hvmonteiro/BitTicker/issues) to see if the enhancement has already been suggested.
- Find out whether your idea fits with the scope and aims of the project.

#### How Do I Submit a Good Enhancement Suggestion?

Enhancement suggestions are tracked as [GitHub issues](https://github.com/hvmonteiro/BitTicker/issues).

- Use a **clear and descriptive title** for the issue to identify the suggestion.
- Provide a **step-by-step description of the suggested enhancement** in as many details as possible.
- **Describe the current behavior** and **explain which behavior you expected to see instead** and why.
- **Explain why this enhancement would be useful** to most BitTicker users.

### Your First Code Contribution

#### Development Environment Setup

1. **Fork the repository** to your GitHub account
2. **Clone your fork** locally:
```
git clone https://github.com/yourusername/BitTicker.git
cd BitTicker
```

3. **Install prerequisites**:
- .NET 6.0 SDK
- Visual Studio 2022 or VS Code
4. **Restore packages**:
```
dotnet restore
```

5. **Build the project**:
```
dotnet build
```

6. **Run the application**:
```
dotnet run
```

#### Making Changes

1. **Create a feature branch**:
```
git checkout -b feature/your-feature-name
```

2. **Make your changes** following the [style guides](#styleguides)
3. **Test your changes** thoroughly
4. **Commit your changes** with a clear commit message
5. **Push to your fork**:
```
git push origin feature/your-feature-name
```

6. **Create a Pull Request** from your fork to the main repository

### Improving The Documentation

Documentation improvements are always welcome! This includes:

- README.md improvements
- Code comments
- API documentation
- Tutorial content
- FAQ additions

## Styleguides

### Commit Messages

- Use the present tense ("Add feature" not "Added feature")
- Use the imperative mood ("Move cursor to..." not "Moves cursor to...")
- Limit the first line to 72 characters or less
- Reference issues and pull requests liberally after the first line

### C# Style Guide

- Follow [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use 4 spaces for indentation
- Use meaningful variable and method names
- Add XML documentation comments for public APIs
- Follow MVVM pattern for WPF components
- Use async/await for I/O operations
- Handle exceptions appropriately


## Attribution

This guide is based on the [contributing-gen](https://github.com/bttger/contributing-gen).
