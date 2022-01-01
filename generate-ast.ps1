[CmdletBinding()]
param (
  [string]
  $OutputPath
)

if ($null -eq $OutputPath) {
  Write-Error "Usage: generate-ast.ps1 <output directory>"
  exit
}

$baseName = "Expr"
# CLASS DEFINITIONS
# ClassName : constructor signature
$classes = @(
  "Binary   : Expr left, Token op, Expr right",
  "Grouping : Expr expression",
  "Literal  : Object value",
  "Unary    : Token op, Expr right"
)

# BEGIN WRITING TO STRINGBUILDER

# usings and namespace
$sb = [System.Text.Stringbuilder]::new()
[void]$sb.AppendLine("using System;")
[void]$sb.AppendLine("using System.Collection.Generic;")
[void]$sb.AppendLine("namespace NLox.Lib")
[void]$sb.AppendLine("{")

# base class def
[void]$sb.AppendLine("  abstract class $BaseName")
[void]$sb.AppendLine("  {")
[void]$sb.AppendLine("  }")

# define subclasses
foreach ($c in $classes) {
  $name, $fields = ($c -split ":") | Foreach-Object { $_.Trim() }
  [void]$sb.AppendLine()
  [void]$sb.AppendLine("  class $name : $baseName")
  [void]$sb.AppendLine("  {")

  # members
  foreach ($field in ($fields -split ", ")) {

    $t, $n = $field -split " "
    [void]$sb.AppendLine("    readonly $t _$n;")
  }

  # constructor
  [void]$sb.AppendLine("    $name($fields)")
  [void]$sb.AppendLine("    {")
  foreach ($field in ($fields -split ", ")) {
    $n = ($field -split " ")[1]
    [void]$sb.AppendLine("    _$n = $n;")
  }
  [void]$sb.AppendLine("    }")

  [void]$sb.AppendLine("  }")
}

# close namespace
[void]$sb.AppendLine("}")

$sb.ToString() | Set-Content "$OutputPath/$baseName.cs"