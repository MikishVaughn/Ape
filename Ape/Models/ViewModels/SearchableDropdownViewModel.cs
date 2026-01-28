using System.Collections.Generic;

namespace Members.Models.ViewModels
{
    public class SearchableDropdownViewModel
    {
        public string FieldName { get; set; } = string.Empty;
        public string LabelText { get; set; } = string.Empty;
        public string Placeholder { get; set; } = "Type to search...";
        public string IconClass { get; set; } = string.Empty;
        public string InputCssClass { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public string? SelectedValue { get; set; }
        public string? DefaultValue { get; set; }
        public bool ShowEmptyOption { get; set; }
        public string EmptyOptionText { get; set; } = "-- None --";
        public bool AllowCustomEntry { get; set; }
        public string CustomEntryText { get; set; } = "Add custom entry";
        public List<SearchableDropdownOption> Options { get; set; } = new List<SearchableDropdownOption>();
    }

    public class SearchableDropdownOption
    {
        public string Value { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string? SubText { get; set; }
        public string? SearchText { get; set; }
        public string? Badge { get; set; }
        public string BadgeClass { get; set; } = "secondary";
        public string? Metadata { get; set; }
    }
}