using System;

namespace P.Pager
{
    /// <summary>
    /// What to do when an <see cref="System.Linq.IQueryable{T}"/> from a database provider is paged without an OrderBy.
    /// Without ordering, databases may return rows in any order, so pages can repeat or skip items.
    /// </summary>
    public enum UnorderedQueryBehavior
    {
        /// <summary>Do nothing (default, matches previous versions).</summary>
        Ignore,

        /// <summary>Write a warning with <see cref="System.Diagnostics.Trace"/>.</summary>
        Warn,

        /// <summary>Throw an <see cref="InvalidOperationException"/>.</summary>
        Throw,
    }

    /// <summary>
    /// Global settings for P.Pager.
    /// </summary>
    public static class PagerSettings
    {
        /// <summary>
        /// How to react when a provider-backed query (for example Entity Framework Core) is paged without an OrderBy.
        /// In-memory sources (<c>List&lt;T&gt;.AsQueryable()</c>, arrays, ...) are always allowed because their order is stable.
        /// Default: <see cref="UnorderedQueryBehavior.Ignore"/>.
        /// </summary>
        public static UnorderedQueryBehavior UnorderedQueryBehavior { get; set; } = UnorderedQueryBehavior.Ignore;

        private static PagerTheme? _defaultTheme;

        /// <summary>
        /// The theme (CSS class names) used by new <c>PagerOptions</c>, and by the Blazor <c>Pagination</c> component once set.
        /// Set it once at startup, for example <c>PagerSettings.DefaultTheme = PagerTheme.Neutral;</c> or <c>PagerTheme.Bootstrap</c>.
        /// Default: <see cref="PagerTheme.Neutral"/> (framework-neutral, styled by <c>pager.css</c>).
        /// Use <see cref="PagerTheme.Classic"/> to keep the markup of P.Pager 9.x.
        /// </summary>
        public static PagerTheme DefaultTheme
        {
            get => _defaultTheme ?? PagerTheme.Neutral;
            set => _defaultTheme = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>True once <see cref="DefaultTheme"/> has been assigned by the app.</summary>
        public static bool IsDefaultThemeSet => _defaultTheme != null;

        /// <summary>Restores the built-in default (<see cref="PagerTheme.Neutral"/>). Mainly for tests.</summary>
        public static void ResetDefaultTheme() => _defaultTheme = null;
    }
}
