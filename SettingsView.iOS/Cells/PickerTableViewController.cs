﻿using System;
using System.Linq.Expressions;
using System.Linq;
using System.Collections.Generic;
using UIKit;
using System.Collections;
using System.Reflection;
using Foundation;
using Xamarin.Forms.Platform.iOS;

namespace AiForms.Renderers.iOS
{
    public class PickerTableViewController:UITableViewController
    {
        
        PickerCell _pickerCell;
        PickerCellView _pickerCellNative;
        SettingsView _parent;
        IList _source;
        Dictionary<int, object> _selectedCache = new Dictionary<int, object>();
        UIColor _accentColor;
        UIColor _titleColor;
        nfloat _fontSize;
        UIColor _background;
        UITableView _tableView;

        public PickerTableViewController(PickerCellView pickerCellView,UITableView tableView)
        {
            _pickerCell = pickerCellView.Cell as PickerCell;
            _pickerCellNative = pickerCellView;
            _parent = pickerCellView.CellParent;
            _source = _pickerCell.ItemsSource as IList;
            _tableView = tableView;

            if(_pickerCell.SelectedItems == null){
                _pickerCell.SelectedItems = new List<object>();
            }

            SetUpProperties();
        }

        void SetUpProperties()
        {
            if (_pickerCell.AccentColor != Xamarin.Forms.Color.Default)
            {
                _accentColor = _pickerCell.AccentColor.ToUIColor();
            }
            else if (_parent.CellAccentColor != Xamarin.Forms.Color.Default)
            {
                _accentColor = _parent.CellAccentColor.ToUIColor();
            }

            if (_pickerCell.TitleColor != Xamarin.Forms.Color.Default)
            {
                _titleColor = _pickerCell.TitleColor.ToUIColor();
            }
            else if (_parent != null && _parent.CellTitleColor != Xamarin.Forms.Color.Default)
            {
                _titleColor = _parent.CellTitleColor.ToUIColor();
            }

            if (_pickerCell.TitleFontSize > 0)
            {
                _fontSize = (nfloat)_pickerCell.TitleFontSize;
            }
            else if (_parent != null)
            {
                _fontSize = (nfloat)_parent.CellTitleFontSize;
            }

            if (_pickerCell.BackgroundColor != Xamarin.Forms.Color.Default)
            {
                _background = _pickerCell.BackgroundColor.ToUIColor();
            }
            else if (_parent != null && _parent.CellBackgroundColor != Xamarin.Forms.Color.Default)
            {
                _background = _parent.CellBackgroundColor.ToUIColor();
            }
        }

        public override UITableViewCell GetCell(UITableView tableView, Foundation.NSIndexPath indexPath)
        {
            
            var reusableCell = tableView.DequeueReusableCell("pikcercell");
            if(reusableCell == null){
                reusableCell = new UITableViewCell(UITableViewCellStyle.Default, "pickercell");

                reusableCell.TextLabel.TextColor = _titleColor;
                reusableCell.TextLabel.Font = reusableCell.TextLabel.Font.WithSize(_fontSize);
                reusableCell.BackgroundColor = _background;
                reusableCell.TintColor = _accentColor;
            }

            var text = _pickerCell.DisplayValue(_source[indexPath.Row]);
            reusableCell.TextLabel.Text = $"{text}";

            reusableCell.Accessory = _selectedCache.ContainsKey(indexPath.Row) ? 
                UITableViewCellAccessory.Checkmark : UITableViewCellAccessory.None;


            return reusableCell;
        }



        public override nint NumberOfSections(UITableView tableView)
        {
            return 1;
        }

        public override nint RowsInSection(UITableView tableView, nint section)
        {
            return _source.Count;
        }

        public override void RowSelected(UITableView tableView, Foundation.NSIndexPath indexPath)
        {
            var cell = tableView.CellAt(indexPath); 

            if(_pickerCell.MaxSelectedNumber == 1){
                RowSelectedSingle(cell,indexPath.Row);
            }
            else{
                RowSelectedMulti(cell,indexPath.Row);
            }

            tableView.DeselectRow(indexPath, true);
        }

        void RowSelectedSingle(UITableViewCell cell,int index)
        {
            if(_selectedCache.ContainsKey(index)){
                return;
            }

            foreach(var vCell in TableView.VisibleCells){
                vCell.Accessory = UITableViewCellAccessory.None;
            }

            _selectedCache.Clear();
            cell.Accessory = UITableViewCellAccessory.Checkmark;
            _selectedCache[index] = _source[index];
        }

        void RowSelectedMulti(UITableViewCell cell, int index)
        {
            if (_selectedCache.ContainsKey(index)) {
                cell.Accessory = UITableViewCellAccessory.None;
                _selectedCache.Remove(index);
                return;
            }

            if (_pickerCell.MaxSelectedNumber != 0 && _selectedCache.Count() >= _pickerCell.MaxSelectedNumber) {
                return;
            }

            cell.Accessory = UITableViewCellAccessory.Checkmark;
            _selectedCache[index] = _source[index];
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);

            Title = _pickerCell.PageTitle;

            var parent = _pickerCell.Parent as SettingsView;
            if(parent != null){
                TableView.SeparatorColor = parent.SeparatorColor.ToUIColor();
                TableView.BackgroundColor = parent.BackgroundColor.ToUIColor();
            }

            foreach(var item in _pickerCell.SelectedItems){
                var idx = _source.IndexOf(item);
                if(idx < 0){
                    continue;
                }
                _selectedCache[idx] = _source[idx];
                if(_pickerCell.MaxSelectedNumber >= 1 && _selectedCache.Count >= _pickerCell.MaxSelectedNumber){
                    break;
                }
            }

            if(_pickerCell.SelectedItems.Count > 0){
                var idx = _source.IndexOf(_pickerCell.SelectedItems[0]);
                BeginInvokeOnMainThread(()=>{
                    TableView.ScrollToRow(NSIndexPath.Create(new nint[] { 0, idx }), UITableViewScrollPosition.Middle, false);
                });
            }

        }

        public override void ViewWillDisappear(bool animated)
        {
            _pickerCell.SelectedItems.Clear();

            foreach(var kv in _selectedCache){
                _pickerCell.SelectedItems.Add(kv.Value);
            }


            _pickerCellNative.UpdateSelectedItems(true);

            if (_pickerCell.KeepSelectedUntilBack)
            {
                _tableView.DeselectRow(_tableView.IndexPathForSelectedRow, true);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if(disposing){
                _pickerCell = null;
                _selectedCache = null;
                _source = null;
                _parent = null;
                _accentColor.Dispose();
                _accentColor = null;
                _titleColor.Dispose();
                _titleColor = null;
                _background.Dispose();
                _background = null;
                _tableView = null;
            }
            base.Dispose(disposing);
        }


    }
}