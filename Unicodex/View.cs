using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using Unicodex.Properties;

namespace Unicodex.View
{
    public class ViewObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class Character : ViewObject
    {
        public Model.Character ModelObject { get; private set; }
        public string Codepoint { get; private set; }
        public string Name { get; private set; }
        public string Value { get; private set; }
        public bool HasSpecialValue { get; private set; }

        public bool IsFavorite
        {
            get
            {
                return ((App)Application.Current).Favorites.IsFavorite(ModelObject.CodepointHex);
            }

            set
            {
                bool changed;
                if (value)
                {
                    changed = ((App)Application.Current).Favorites.AddFavorite(ModelObject.CodepointHex);
                }
                else
                {
                    changed = ((App)Application.Current).Favorites.RemoveFavorite(ModelObject.CodepointHex);
                }
                if (changed)
                {
                    Settings.Default.Save();
                    RaisePropertyChanged();
                }
            }
        }

        public List<View.Tag> Tags
        {
            get
            {
                return Model.Tag.ToView(((App)Application.Current).TagGroups.GetTags(ModelObject.CodepointHex));
            }
        }

        public Character(Model.Character c)
        {
            ModelObject = c;
            Codepoint = "U+" + c.CodepointHex;
            Name = c.Name;

            if (c.Codepoint <= 32)
            {
                Value = char.ConvertFromUtf32(0x2400 + c.Codepoint);
                HasSpecialValue = true;
            }
            else if (c.Codepoint == 127)
            {
                Value = "\u2421";
                HasSpecialValue = true;
            }
            else
            {
                Value = c.Value;
                HasSpecialValue = false;
            }
        }
    }

    public class Tag : ViewObject
    {
        public string Name { get; private set; }
        public string Source { get; private set; }
        public int Count { get; private set; }

        public string Description
        {
            get
            {
                return Source + ", " + Count + " character" + (Count == 1 ? "" : "s");
            }
        }

        public Tag(Model.Tag tag)
        {
            Name = tag.TagName;
            Source = tag.TagGroup.Source;
            Count = tag.TagGroup.TagToCodepoints[Name].Count;
        }
    }
}
