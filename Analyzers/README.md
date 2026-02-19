# Analyzers directory

This directory contains roslyn analyzers that are used to analyze Avatar Optimizer code in development environments.

Those analyzers can be used to detect potential issues, prevents common mistakes, and enforce best practices when writing code for Avatar Optimizer.

Those analyzers are generally came from NuGet packages and are not developed by us, and they are published under their respective licenses.
Please refer to the documentation of each analyzer for more information on their usage and licensing.

Most of analyzer rules are enabled as warning.
Thanks to -warnaserror option in csc.rsp, all warnings are treated as errors, which means that any code that violates the rules will fail to compile.
However, not all rules are tested by us, AAO team, so you may encounter some false positives or annoying errors.

If you encounter such issues, you can try to disable the rule for entire project in `.AvatarOptimizer.ruleset` file.
I'll consider if we should disable the rule in the default ruleset file or suppress with directives in code.

This directory and analyzers are removed from final build and are only used during development, so they won't have any impact compilation on user environments.
