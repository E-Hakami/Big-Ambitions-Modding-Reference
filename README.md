# Big Ambitions Modding Reference

An unofficial reference for understanding the internal C# code of **Big Ambitions** and its modding-related systems.

The repository contains decompiled C# source references from specific game builds, along with automatically generated Markdown pages that make the code easier to navigate.

> **This is an unofficial community reference and is not affiliated with the developers of Big Ambitions.**

---

# Assembly

The Assembly directory contains the C# source references for that build.

The files are obtained by decompiling the game's compiled C# assemblies.

For example:

Assembly/
└── BigAmbitions.ModsInternal/
    └── Classes/
        └── BAModAPI/
            └── ModActivationScope.cs

The .cs files contain the technical details of the classes, including things such as:

Classes
Methods
Fields
Interfaces
Inheritance
Events

These files are the primary technical reference.

# Docs

The Docs directory mirrors the structure of Assembly.

For example:

Assembly/
└── BigAmbitions.ModsInternal/
    └── Classes/
        └── BAModAPI/
            └── ModActivationScope.cs

has a corresponding:

Docs/
└── BigAmbitions.ModsInternal/
    └── Classes/
        └── BAModAPI/
            └── ModActivationScope.md

Each Markdown file provides an easier way to navigate the corresponding C# file and its relationships with other classes.

Using the Documentation

Each generated .md file contains four main sections.

# ClassName

## Main Reference

## Uses

## Used By

## Notes


# Main Reference

Contains a link to the corresponding .cs source file.

Use this when you want to inspect the actual class definition and implementation.

# Uses

Lists other classes that the current class references.

For example:

EmployeeManager

Uses:
- Employee
- EmployeeData
- EmployeeTask

Each entry links to that class's Markdown documentation.

This allows you to follow relationships through the codebase.

# Used By

Lists classes that reference the current class.

For example:

EmployeeData

Used By:
- EmployeeManager
- BusinessManager

This is the reverse of Uses.

If:

A Uses B

then:

B Used By A

This is useful when you want to find out which parts of the game interact with a particular class.

# Notes

The Notes section is reserved for additional information that cannot be automatically determined from the source.

It is currently left empty by the generator.

It can be used for things such as:

Explanations of what a class appears to do
Important observations
Modding-related information
Known limitations
Examples
Research notes
How to Use the Reference

**For important information, always check the original .cs reference.**

# Disclaimer

This is an unofficial, community-created reference for Big Ambitions.

It is not affiliated with or endorsed by the developers or publishers of Big Ambitions.

The information may be incomplete or inaccurate, and internal code may change between game versions.

Use the reference corresponding to the game build you are working with.
