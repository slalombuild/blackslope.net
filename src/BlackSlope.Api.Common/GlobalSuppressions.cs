// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.
using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1101:PrefixLocalCallsWithThis", Justification = "BlackSlope currently prefixes local class field names with underscores.")]
[assembly: SuppressMessage("StyleCop.CSharp.NamingRules", "SA1309:FieldNamesMustNotBeginWithUnderscore", Justification = "BlackSlope uses underscore notation to identify local class fields.")]

[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1629:DocumentationTextMustEndWithAPeriod", Justification = "Too pedantic.")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1633:FileMustHaveHeader", Justification = "Pending.")]

// Remove this when read to begin documenting
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "To be done later.")]

// Remove these when ready to document parameters and return statements
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1614:ElementParameterDocumentationMustHaveText", Justification = "To be done later.")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1616:ElementReturnValueDocumentationMustHaveText", Justification = "To be done later.")]
[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Global exception logger", Scope = "member", Target = "~M:BlackSlope.Api.Common.Middleware.ExceptionHandling.ExceptionHandlingMiddleware.Invoke(Microsoft.AspNetCore.Http.HttpContext)~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "Composite makes the intention clearer", Scope = "type", Target = "~T:BlackSlope.Api.Common.Validators.CompositeValidator`1")]
[assembly: SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Class organization", Scope = "member", Target = "~M:BlackSlope.Api.Common.Exceptions.HandledException.#ctor(BlackSlope.Api.Common.Exceptions.ExceptionType,System.String,System.Net.HttpStatusCode,System.String)")]
