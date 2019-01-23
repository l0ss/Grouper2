<img align="right" width="128" src="Docs/Images/CsConsoleFormatIcon256.png" style="margin: 0 20px">

*CsConsoleFormat: advanced formatting of console output for .NET*
=================================================================

* [**GitHub repository**](https://github.com/Athari/CsConsoleFormat)
* [**NuGet package**](https://www.nuget.org/packages/Alba.CsConsoleFormat)
<!-- -->
    PM> Install-Package Alba.CsConsoleFormat

[![GitHub license](https://img.shields.io/github/license/Athari/CsConsoleFormat.svg?logo=github)](https://github.com/Athari/CsConsoleFormat/releases)
[![GitHub license](https://img.shields.io/github/languages/top/Athari/CsConsoleFormat.svg?logo=github)](https://github.com/Athari/CsConsoleFormat/releases)
[![GitHub license](https://img.shields.io/github/languages/code-size/Athari/CsConsoleFormat.svg?logo=github)](https://github.com/Athari/CsConsoleFormat/releases)
[![Badges](https://img.shields.io/badge/badges-19%20/%2019-lightgrey.svg?logo=data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAA4AAAAOCAYAAAAfSC3RAAAAGXRFWHRTb2Z0d2FyZQBBZG9iZSBJbWFnZVJlYWR5ccllPAAAAExJREFUeNpizM3NdWBgYFgIxHIMaEArdjIDFvAIiOOZgMRsbJrwAJDa2SCNKgykAxUmBjLBqMbho/EOGfoegjSmAvFdEjSB1CYABBgAwJEKs3F%2BVV0AAAAASUVORK5CYII=)](http://shields.io/)
<br>
[![AppVeyor build master](https://img.shields.io/appveyor/ci/athari/csconsoleformat/master.svg?logo=appveyor)](https://ci.appveyor.com/project/Athari/csconsoleformat/branch/master)
[![AppVeyor tests master](https://img.shields.io/appveyor/tests/athari/csconsoleformat/master.svg?logo=appveyor)](https://ci.appveyor.com/project/Athari/csconsoleformat/branch/master/tests)
<br>
[![GitHub release version](https://img.shields.io/github/release/Athari/CsConsoleFormat.svg?logo=github)](https://github.com/Athari/CsConsoleFormat/releases)
[![GitHub release date](https://img.shields.io/github/release-date/Athari/CsConsoleFormat.svg?logo=github)](https://github.com/Athari/CsConsoleFormat/releases)
[![GitHub commits since release](https://img.shields.io/github/commits-since/Athari/CsConsoleFormat/latest.svg?logo=github)](https://github.com/Athari/CsConsoleFormat/releases)
[![GitHub open issues](https://img.shields.io/github/issues-raw/Athari/CsConsoleFormat.svg?logo=github)](https://github.com/Athari/CsConsoleFormat/issues)
[![GitHub closed issues](https://img.shields.io/github/issues-closed-raw/Athari/CsConsoleFormat.svg?logo=github)](https://github.com/Athari/CsConsoleFormat/issues)
[![GitHub pull requests](https://img.shields.io/github/issues-pr/Athari/CsConsoleFormat.svg?logo=github)](https://github.com/Athari/CsConsoleFormat/pulls)
<br>
[![NuGet release version](https://img.shields.io/nuget/v/Alba.CsConsoleFormat.svg?label=release&logo=data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAA4AAAAOCAYAAAAfSC3RAAAAGXRFWHRTb2Z0d2FyZQBBZG9iZSBJbWFnZVJlYWR5ccllPAAAAMpJREFUeNqUkbENwjAQRY8ovZkgG5AJYALS0GaDMAGeACYIE7ABtFSZgEyAy3TegG/pR7IuToCTXuHTvTufvTo8h42InIAFTn6MDARxCwr5I3JOuYAuyhdsqCPU%2BFHslHQD9cKwI7jmKtlSsrz%2BPiGGGhOLBjSgAg/m3jO7nzMlSrSbiXLJV510A3dOi0X/TRTuNko9KMGaayyKcVSUhbtbLfoZ0SX%2BciL2CbFR5/DPXl91l5jcsjg0eHH3UouecqfyNRsIH8p9BBgAANInlRmoOxQAAAAASUVORK5CYII=)](https://www.nuget.org/packages/Alba.CsConsoleFormat)
[![NuGet downloads](https://img.shields.io/nuget/dt/Alba.CsConsoleFormat.svg?logo=data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAA4AAAAOCAYAAAAfSC3RAAAAGXRFWHRTb2Z0d2FyZQBBZG9iZSBJbWFnZVJlYWR5ccllPAAAAMpJREFUeNqUkbENwjAQRY8ovZkgG5AJYALS0GaDMAGeACYIE7ABtFSZgEyAy3TegG/pR7IuToCTXuHTvTufvTo8h42InIAFTn6MDARxCwr5I3JOuYAuyhdsqCPU%2BFHslHQD9cKwI7jmKtlSsrz%2BPiGGGhOLBjSgAg/m3jO7nzMlSrSbiXLJV510A3dOi0X/TRTuNko9KMGaayyKcVSUhbtbLfoZ0SX%2BciL2CbFR5/DPXl91l5jcsjg0eHH3UouecqfyNRsIH8p9BBgAANInlRmoOxQAAAAASUVORK5CYII=)](https://www.nuget.org/packages/Alba.CsConsoleFormat)
[![MyGet pre-release version](https://img.shields.io/myget/athari/vpre/Alba.CsConsoleFormat.svg?label=pre-release&logo=data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAA4AAAAOCAYAAAAfSC3RAAAAGXRFWHRTb2Z0d2FyZQBBZG9iZSBJbWFnZVJlYWR5ccllPAAAAGZJREFUeNpi/P//PwM5gJFsjXKtDAFAmheINYFYGoiloLQmTI28pDHI9EVAvB6IN4AEWaAcYkAcFH8C4r1MZLiSD4gDmRjIBKMacYNTLNC4AQWxBjTikTEy2ANNBIspS3LkagQIMADeAxhiTKPYOgAAAABJRU5ErkJggg==)](https://www.myget.org/feed/Packages/athari)
[![MyGet downloads](https://img.shields.io/myget/athari/dt/Alba.CsConsoleFormat.svg?logo=data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAA4AAAAOCAYAAAAfSC3RAAAAGXRFWHRTb2Z0d2FyZQBBZG9iZSBJbWFnZVJlYWR5ccllPAAAAGZJREFUeNpi/P//PwM5gJFsjXKtDAFAmheINYFYGoiloLQmTI28pDHI9EVAvB6IN4AEWaAcYkAcFH8C4r1MZLiSD4gDmRjIBKMacYNTLNC4AQWxBjTikTEy2ANNBIspS3LkagQIMADeAxhiTKPYOgAAAABJRU5ErkJggg==)](https://www.myget.org/feed/Packages/athari)
<br>
[![FOSSA license scan](https://app.fossa.io/api/projects/git%2Bgithub.com%2FAthari%2FCsConsoleFormat.svg?type=shield&logo=data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAwAAAAOBAMAAADpk%2BDfAAAALVBMVEUAAADm5ubm5ubm5ubm5ubm5ubm5ubm5ubm5ubm5ubm5ubm5ubm5ubm5ubm5uaIA73MAAAADnRSTlMAtluj7Np/bTaRJEgSyDlZ5%2BgAAABnSURBVAjXY2A7wMDQqsDAVpbU47eBgSvO7pkdAwPTwxBLIwYG7mc8nhMYGBYKi4iKMzCoTxR8cnUBQ9HEWPF3Ygxt0iZP7IQZOBboPbSLYWBg6IuVUwBS%2B%2BTEGYBAT2QBiFJsYGAAAPgsF6HL6FZzAAAAAElFTkSuQmCC)](https://app.fossa.io/projects/git%2Bgithub.com%2FAthari%2FCsConsoleFormat)
[![CII best practices](https://bestpractices.coreinfrastructure.org/projects/1679/badge)](https://bestpractices.coreinfrastructure.org/projects/1679)
[![Libraries.io Dependencies](https://img.shields.io/librariesio/github/Athari/CsConsoleFormat.svg?logo=data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAA4AAAAOBAMAAADtZjDiAAAAHlBMVEUAAADm5ubm5ubm5ubm5ubm5ubm5ubm5ubm5ubm5uZenlmZAAAACXRSTlMAVTjixqocjXFWMi4dAAAAUUlEQVQI12NwNjI2KglgYJg5ceb0mRNANBBOAtOTLKdD6MrJEFpyZgKY5pwpAKZZZgaAaY6ZBmCaaeY0MM0wcwaEjpwKoTtnMjibOZsxJAcCAI4KK2mmVnlHAAAAAElFTkSuQmCC)](https://libraries.io/nuget/Alba.CsConsoleFormat)

CsConsoleFormat is a library for formatting text in console based on documents resembling a mix of WPF and HTML: tables, lists, paragraphs, colors, word wrapping, lines etc. Like this:

```xml
<Document>
    <Span Color="Red">Hello</Span>
    <Br/>
    <Span Color="Yellow">world!</Span>
</Document>
```

or like this:

```c#
new Document(
    new Span("Hello") { Color = ConsoleColor.Red },
    "\n",
    new Span("world!") { Color = ConsoleColor.Yellow }
);
```

or even like this:

```c#
Colors.WriteLine("Hello".Red(), "\n", "world!".Yellow());
```

Why?
====

.NET Framework includes only very basic console formatting capabilities. If you need to output a few strings, it's fine. If you want to output a table, you have to calculate column widths manually, often hardcode them. If you want to color output, you have to intersperse writing strings with setting and restoring colors. If you want to wrap words properly or combine all of the above...

The code quickly becomes an unreadable mess. It's just not fun! In GUI, we have MV*, bindings and all sorts of cool stuff. Writing console applications feels like returning to the Stone Age.

*CsConsoleFormat to the rescue!*

Imagine you have the usual Order, OrderItem and Customer classes. Let's create a document which prints the order. There're two major syntaxes, you can use either.

**XAML** (like WPF):

```xml
<Document xmlns="urn:alba:cs-console-format"
          xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Span Background="Yellow" Text="Order #"/>
    <Span Text="{Get OrderId}"/>
    <Br/>
    <Span Background="Yellow" Text="Customer: "/>
    <Span Text="{Get Customer.Name}"/>

    <Grid Color="Gray">
        <Grid.Columns>
            <Column Width="Auto"/>
            <Column Width="*"/>
            <Column Width="Auto"/>
        </Grid.Columns>
        <Cell Stroke="Single Double" Color="White">Id</Cell>
        <Cell Stroke="Single Double" Color="White">Name</Cell>
        <Cell Stroke="Single Double" Color="White">Count</Cell>
        <Repeater Items="{Get OrderItems}">
            <Cell>
                <Span Text="{Get Id}"/>
            </Cell>
            <Cell>
                <Span Text="{Get Name}"/>
            </Cell>
            <Cell Align="Right">
                <Span Text="{Get Count}"/>
            </Cell>
        </Repeater>
    </Grid>
</Document>
```

```c#
// Assuming Order.xaml is stored as an Embedded Resource in the Views folder.
Document doc = ConsoleRenderer.ReadDocumentFromResource(GetType(), "Views.Order.xaml", Order);
ConsoleRenderer.RenderDocument(doc);
```

**C#** (like LINQ to XML):

```c#
using static System.ConsoleColor;

var headerThickness = new LineThickness(LineWidth.Double, LineWidth.Single);

var doc = new Document(
    new Span("Order #") { Color = Yellow }, Order.Id, "\n",
    new Span("Customer: ") { Color = Yellow }, Order.Customer.Name,
    new Grid {
        Color = Gray,
        Columns = { GridLength.Auto, GridLength.Star(1), GridLength.Auto },
        Children = {
            new Cell("Id") { Stroke = headerThickness },
            new Cell("Name") { Stroke = headerThickness },
            new Cell("Count") { Stroke = headerThickness },
            Order.OrderItems.Select(item => new[] {
                new Cell(item.Id),
                new Cell(item.Name),
                new Cell(item.Count) { Align = Align.Right },
            })
        }
    }
);

ConsoleRenderer.RenderDocument(doc);
```

**C#** (like npm/colors):

```c#
using Alba.CsConsoleFormat.Fluent;

Colors.WriteLine(
    "Order #".Yellow(), Order.Id, "\n",
    "Customer: ".Yellow(), Order.Customer.Name,
    // the rest is the same
);
```

Features
========

* **HTML-like elements**: paragraphs, spans, tables, lists, borders, separators.
* **Layouts**: grid, stacking, docking, wrapping, absolute.
* **Text formatting**: foreground and background colors, character wrapping, word wrapping.
* **Unicode formatting**: hyphens, soft hyphens, no-break hyphens, spaces, no-break spaces, zero-width spaces.
* **Multiple syntaxes** (see examples above):
    * **Like WPF**: XAML with one-time bindings, resources, converters, attached properties, loading documents from assembly resources.
    * **Like LINQ to XML**: C# with object initializers, setting attached properties via extension methods or indexers, adding children elements by collapsing enumerables and converting objects and strings to elements.
    * **Like npm/colors**: Limited to writing colored strings, but very concise. Can be combined with the general syntax above.
* **Drawing**: geometric primitives (lines, rectangles) using box-drawing characters, color transformations (dark, light), text, images.
* **Internationalization**: cultures are respected on every level and can be customized per-element.
* **Export** to many formats: ANSI text, unformatted text, HTML; RTF, XPF, WPF FixedDocument, WPF FlowDocument.
* **JetBrains ReSharper annotations**: CanBeNull, NotNull, ValueProvider, Pure etc.
* **WPF** document control, document converter.

Getting started
===============

1. Install NuGet package [Alba.CsConsoleFormat](https://www.nuget.org/packages/Alba.CsConsoleFormat) using Package Manager:

        PM> Install-Package Alba.CsConsoleFormat

    or .NET CLI:

        > dotnet add package Alba.CsConsoleFormat

2. Add `using Alba.CsConsoleFormat;` to your .cs file.

3. If you're going to use ASCII graphics on Windows, set `Console.OutputEncoding = Encoding.UTF8;`.

4. If you want to use XAML:

    1. Add XAML file to your project. Set its build action to "Embedded Resource".
    2. Load XAML using `ConsoleRenderer.ReadDocumentFromResource`.

5. If you want to use pure C#:

    1. Build a document in code starting with `Document` element as a root.

6. Call `ConsoleRenderer.RenderDocument` on the generated document.

Choosing syntax
===============

**XAML** (like WPF) forces clear separation of views and models which is a good thing. However, it isn't strongly typed, so it's easy to get runtime errors if not careful. Syntax-wise it's a combination of XML verbosity (`<Grid><Grid.Columns><Column/></Grid.Columns></Grid>`) and conciseness of short enums (`Color="White"`) and converters (`Stroke="Single Double"`).

XAML library in Mono is currently very buggy. If you want to build a cross-platform application, using XAML may be problematic. However, if you need to support only Windows and are experienced in WPF, XAML should feel natural.

XAML is only partially supported by Visual Studio + ReSharper: syntax highlighting and code completion work, but library-specific markup extensions are't understood by code completion, so incorrect errors may be displayed.

**C#** (like LINQ to XML) allows performing all sorts of transformations with objects right in the code, thanks to LINQ and collapsing of enumerables when adding children elements. When using C# 6, which supports `using static`, accessing some of enumerations can be shortened. The only place with loose typing is adding of children using collection initializer of `Element.Children` (or constructor of `Element`).

Building documents in code is fully supported by IDE, but code completion may cause lags if documents are built with huge single statements.

Framework Compatibility
=======================

The library contains the following packages:
* **Alba.CsConsoleFormat**: main library with XAML and bindings support.
* **Alba.CsConsoleFormat-NoXaml**: main library without XAML and bindings support (also without Repeater element).
* **Alba.CsConsoleFormat.Presentation**: WPF-dependent features, including WPF control, export to RTF etc.
* **Alba.CsConsoleFormat.ColorfulConsole**: support for Coloful.Console's FIGlet fonts.
* **Alba.CsConsoleFormat.ColorfulConsole-NoXaml**: Alba.CsConsoleFormat.ColorfulConsole which depends on Alba.CsConsoleFormat-NoXaml.

The library supports the following targets:

* **.NET Standard 2.0** *(Windows Vista; Core 2.0; Mono 5.4)*
    * Alba.CsConsoleFormat (depends in Portable.Xaml)
    * Alba.CsConsoleFormat-NoXaml
    * Alba.CsConsoleFormat.ColorfulConsole
    * Alba.CsConsoleFormat.ColorfulConsole-NoXaml
* **.NET Standard 1.3** *(Windows Vista; Core 1.0; Mono 4.6)*
    * Alba.CsConsoleFormat (depends in Portable.Xaml)
    * Alba.CsConsoleFormat-NoXaml
    * Alba.CsConsoleFormat.ColorfulConsole
    * Alba.CsConsoleFormat.ColorfulConsole-NoXaml
* **.NET Framework 4.0** *(Windows Vista)*
    * Alba.CsConsoleFormat (depends in System.Xaml)
    * Alba.CsConsoleFormat-NoXaml
    * Alba.CsConsoleFormat.Presentation
    * Alba.CsConsoleFormat.ColorfulConsole
    * Alba.CsConsoleFormat.ColorfulConsole-NoXaml
* **.NET Framework 3.5** *(Windows XP)*
    * Alba.CsConsoleFormat-NoXaml

*Notes:*

1. Alba.CsConsoleFormat can be ported to .NET Framework 3.5 if someone actually needs it. It's just not worth the hassle otherwise as it requires changes to Portable.Xaml library.
2. Alba.CsConsoleFormat-NoXaml can be supported on .NET Standard 1.0, Windows Phone 8.0 and other platforms, but they don't support console. WPF control belongs to the "just for fun" genre, but if somebody actually needs something like this on other platforms, it can be ported.

License
=======
Copyright © 2014–2018 Alexander "Athari" Prokhorov

Licensed under the [Apache License, Version 2.0](License.md) (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

<http://www.apache.org/licenses/LICENSE-2.0>

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

Some parts of the library are based on ConsoleFramework © Igor Kostomin under MIT license.

Related Projects
================

* [**ConsoleFramework**](http://elw00d.github.io/consoleframework) — fully-featured cross-platform console user interface framework. Using ConsoleFramework, you can create interactive user interface with mouse input, but its formatting capabilities are limited.

    CsConsoleFormat includes more formatting features: inline text with support for Unicode, more layouts, more everything, so if you only need to output text, CsConsoleFormat is more appropriate. However, if you want an interactive user interface with windows, menus and buttons, ConsoleFramework is the only library out there which supports it.

* [**Colorful.Console**](https://github.com/tomakita/Colorful.Console) — library for coloring text in console and ASCII-art fonts. Supports RGB colors on Windows, falls back to 16 colors on other platforms.

    Colorful.Console offers more coloring features: RGB colors, text highlighting based on regular expressions, gradients. However, it falls flat if anything beyond coloring is required as its output is based on calling Console.WriteLine variations, so it suffers from the same basic problems as the standard System.Console. FIGlet fonts are supported by CsConsoleFormat through Colorful.Console, but there's no integration beyond that.

* [**ConsoleTables**](https://github.com/khalidabuhakmeh/ConsoleTables) — simple library for printing tables. (There're several alternatives, but this one is the most popular.)

    Almost all features of ConsoleTables are trivial to implement with CsConsoleFormat. ConsoleTables is limited to single-line cells and a few fixed formatting styles.

Links
=====

TODO