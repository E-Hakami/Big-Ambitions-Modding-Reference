# Big Ambitions Modding Reference

An unofficial, versioned, reverse-engineered technical reference for
**Big Ambitions** mod development.

This repository is intended to make the internal structure of Big Ambitions
easier to understand for **mod developers, programmers, researchers, and AI
assistants**.

It documents the game's compiled C# assemblies by organizing their classes,
relationships, dependencies, and source references into a browsable structure.

> **This is not an official Big Ambitions API reference.**
>
> It is an independently created reference based on examination of the game's
> compiled assemblies. The information may be incomplete, inaccurate, or
> specific to a particular game build.

---

# Table of Contents

- [What Is This?](#what-is-this)
- [Why Does This Exist?](#why-does-this-exist)
- [Who Is This For?](#who-is-this-for)
- [Repository Structure](#repository-structure)
- [Build Versioning](#build-versioning)
- [Assembly Directory](#assembly-directory)
- [Documentation Directory](#documentation-directory)
- [How the Markdown References Work](#how-the-markdown-references-work)
- [Main Reference](#main-reference)
- [Uses](#uses)
- [Used By](#used-by)
- [Notes](#notes)
- [Dependency Graph](#dependency-graph)
- [The Documentation Generator](#the-documentation-generator)
- [How Dependencies Are Detected](#how-dependencies-are-detected)
- [Why the Source Files Are Included](#why-the-source-files-are-included)
- [Why Markdown Files Are Included](#why-markdown-files-are-included)
- [How to Navigate the Reference](#how-to-navigate-the-reference)
- [Example](#example)
- [Version Differences](#version-differences)
- [Accuracy and Limitations](#accuracy-and-limitations)
- [Reverse Engineering Methodology](#reverse-engineering-methodology)
- [Intended Use](#intended-use)
- [What This Repository Is Not](#what-this-repository-is-not)
- [Contributing](#contributing)
- [Future Plans](#future-plans)
- [Disclaimer](#disclaimer)

---

# What Is This?

The **Big Ambitions Modding Reference** is a technical knowledge base for
understanding the internal C# code structure of Big Ambitions.

Big Ambitions contains a large amount of functionality implemented inside
compiled C# assemblies. When developing mods, it can be useful to know:

- What classes exist
- Where a class is located
- What other classes it interacts with
- Which classes depend on it
- What methods and fields are available
- What namespaces and systems are involved
- How different parts of the game are connected
- Which implementation details change between game builds

This repository attempts to organize that information into a structure that is
easy for both humans and machines to navigate.

The project therefore contains two closely related types of information:

1. **C# source references**
2. **Markdown documentation and relationships**

The C# files provide the technical reference.

The Markdown files provide the navigational and conceptual layer.

Together they form a searchable map of the game's internal code.

---

# Why Does This Exist?

Modding a game can require understanding systems that are not necessarily
documented for mod developers.

Official modding documentation can explain how to create a mod, but it may not
explain every internal class involved in a particular game system.

For example, a mod developer might want to understand:

```text
How are employees represented?

Which class controls an employee?

What classes does that class depend on?

What other systems use it?

Where is the relevant implementation?

What methods can be called?

What happens when a particular method is executed?
