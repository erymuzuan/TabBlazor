﻿using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TabBlazor
{
    public partial class ItemSelect<TItem, TValue> : TablerBaseComponent
    {
        [Parameter] public IEnumerable<TItem> Items { get; set; }
        [Parameter] public string NoSelectedText { get; set; } = "*Select*";
        [Parameter] public string NoItemsText { get; set; }
        [Parameter] public bool ShowCheckBoxes { get; set; }
        [Parameter] public bool MultiSelect { get; set; }

        [Parameter] public List<TValue> SelectedValues { get; set; }
        [Parameter] public EventCallback<List<TValue>> SelectedValuesChanged { get; set; }

        [Parameter] public TValue SelectedValue { get; set; }
        [Parameter] public EventCallback<TValue> SelectedValueChanged { get; set; }

        [Parameter] public EventCallback Changed { get; set; }

        [Parameter] public Func<TItem, string> SelectedTextExpression { get; set; }
        [Parameter] public Func<TItem, TValue> ConvertExpression { get; set; }
        [Parameter] public RenderFragment<TItem> ListTemplate { get; set; }
        [Parameter] public RenderFragment<List<TItem>> SelectedTemplate { get; set; }
        [Parameter] public RenderFragment FooterTemplate { get; set; }
        [Parameter] public bool Clearable { get; set; }
        [Parameter] public bool Disabled { get; set; }
        [Parameter] public bool RemoveSelectedFromList { get; set; }
        [Parameter] public int MaxSelectableItems { get; set; } = int.MaxValue;
        [Parameter] public Func<string, IEnumerable<TItem>> SearchMethod { get; set; }
        [Parameter] public string SearchPlaceholderText { get; set; }
        [Parameter] public string MaxListHeight { get; set; }
        [Parameter] public string Label { get; set; }

        private bool showSearch => SearchMethod != null;
        private bool singleSelect => MaxSelectableItems == 1 || !MultiSelect;
        private List<TItem> selectedItems = new();
        private Dropdown dropdown;
        private string searchText;

        protected override void OnInitialized()
        {
            if (ConvertExpression == null)
            {
                if (typeof(TItem) != typeof(TValue))
                {
                    throw new InvalidOperationException($"{GetType()} requires a {nameof(ConvertExpression)} parameter.");
                }

                ConvertExpression = item => item is TValue value ? value : default;
            }
        }

        protected override void OnParametersSet()
        {
            if (selectedItems == null) { selectedItems = new List<TItem>(); }
            selectedItems.Clear();

            //TODO How to handle if the items are in the provided list
            if (MultiSelect && SelectedValues != null)
            {
                if (typeof(TItem) == typeof(TValue))
                {
                    selectedItems = SelectedValues.Cast<TItem>().ToList();
                }
                else
                {
                    foreach (var selectedValue in SelectedValues)
                    {
                        AddSelectItemFromValue(selectedValue);
                    }
                }
            }
            else if (!MultiSelect && SelectedValue != null)
            {
                AddSelectItemFromValue(SelectedValue);
            }


        }

        private void AddSelectItemFromValue(TValue value)
        {
            var item = Items.FirstOrDefault(e => EqualityComparer<TValue>.Default.Equals(ConvertExpression(e), value));
            if (item != null)
            {
                selectedItems.Add(item);
            }
        }

        protected List<TItem> FilteredList()
        {
            var filtered = Items;
            if (SearchMethod != null && !string.IsNullOrWhiteSpace(searchText))
            {
                filtered = SearchMethod(searchText).ToList();
            }

            if (RemoveSelectedFromList)
            {
                return filtered.Where(e => !selectedItems.Contains(e)).ToList();
            }
            return filtered.ToList();
        }

        private void ClearSearch()
        {
            searchText = string.Empty;
        }

        private string GetSelectedText(TItem item)
        {
            if (SelectedTextExpression == null) return item.ToString();
            return SelectedTextExpression.Invoke(item);
        }

        private bool CanSelect()
        {
            return singleSelect || MaxSelectableItems > selectedItems.Count;
        }

        private TValue GetValue(TItem item)
        {
            return ConvertExpression.Invoke(item);
        }

        private bool IsSelected(TItem item)
        {

            return selectedItems.Contains(item);
        }

        protected async Task RemoveSelected(TItem item)
        {
            if (IsSelected(item))
            {
                selectedItems.Remove(item);
            }
            dropdown.Close();
            await UpdateChanged();
        }

        public async Task ClearSelected()
        {
            selectedItems.Clear();
            dropdown.Close();
            await UpdateChanged();
        }

        protected async Task ToogleSelected(TItem item)
        {
            if (singleSelect)
            {
                selectedItems.Clear();
            }

            if (IsSelected(item))
            {
                selectedItems.Remove(item);
            }
            else
            {
                selectedItems.Add(item);

                if (singleSelect || !CanSelect())
                {
                    dropdown.Close();
                }
            }

            await UpdateChanged();
        }

        private async Task UpdateChanged()
        {
            //Allways send out SelectedValuesChanged
            var selectedValues = new List<TValue>();
            selectedValues = selectedItems.Select(e => GetValue(e)).ToList();
            await SelectedValuesChanged.InvokeAsync(selectedValues);

            if (!MultiSelect)
            {
                await SelectedValueChanged.InvokeAsync(selectedValues.FirstOrDefault());
            }

            await Changed.InvokeAsync();
        }

    }
}
