// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "Test project", Scope = "member", Target = "~M:ServiceBus.Tests.When_starting_the_function_host.Should_not_blow_up~System.Threading.Tasks.Task")]
