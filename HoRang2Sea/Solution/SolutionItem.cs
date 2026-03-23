using DevExpress.Diagram.Core;
using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.Native;
using DevExpress.Mvvm.POCO;
using HoRang2Sea.Models;
using HoRang2Sea.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace HoRang2Sea
{
    public enum SolutionType { GRID, XYCHART, FishingBoat, PortGuideShip, Con14ton, TrainingShip }
    [POCOViewModel]
    [XmlType(TypeName = "SolutionItem")]
    public class SolutionItem
    {
        protected SolutionItem()
        {
            Items = new ObservableCollection<SolutionItem>();
        }
        public Mymodel mymodel { get; set; }
        public SolutionType Type { get; set; }
        public string Name { get; set; }
        public string FilePath { get; set; } = "";
        public string GlyphPath { get; set; }
        public bool IsFile { get { return FilePath != null; } }
        public PanelWorkspaceViewModel Workspace { get; set; }
        public ObservableCollection<SolutionItem> Items { get; set; }
        public static SolutionItem Create(string displayName, string glyph, SolutionType type)
        {
            var solutionItem = ViewModelSource.Create(() => new SolutionItem());
            solutionItem.Do(x =>
            {
                x.Name = displayName;
                x.GlyphPath = glyph;
                x.Type = type;
                x.mymodel = x.GetMymodel(type);
                x.Items = x.Items;
            });
            return solutionItem;
        }
        public static SolutionItem CreateFile(string path, SolutionType type)
        {
            string glyph = "";
            var solutionItem = ViewModelSource.Create(() => new SolutionItem());
            solutionItem.Do(x =>
            {
                x.Name = Path.GetFileName(path);
                x.GlyphPath = glyph;
                x.FilePath = path;
                x.Type = type;
                x.mymodel = x.GetMymodel(type);

            });
            return solutionItem;
        }
        public static SolutionItem Create(SolutionItem item)
        {
            var solutionItem = ViewModelSource.Create(() => new SolutionItem());
            solutionItem.Do(x =>
            {
                x.Name = item.Name;
                x.GlyphPath = item.GlyphPath;
                x.FilePath = item.FilePath;
                x.Type = item.Type;
                x.mymodel = item.mymodel;
            });
            return solutionItem;
        }

        public Mymodel GetMymodel(SolutionType type)
        {
            Mymodel mymodel = null;
            switch (type)
            {
                case SolutionType.GRID:
                    mymodel = new GridModel();
                    break;
                case SolutionType.XYCHART:
                    mymodel = new ChartModel();
                    break;
                case SolutionType.FishingBoat:
                    mymodel = new FishingBoatModel();
                    break;
                case SolutionType.PortGuideShip:
                    mymodel = new PortGuideShipModel();
                    break;
                case SolutionType.TrainingShip:
                    mymodel = new TrainingShipModel();
                    break;


                default:
                    break;
            }
            return mymodel;
        }
        public void PostRead()
        {
            var copieditems = Items.ToList();
            Items.Clear();
            foreach (var de in copieditems)
            {
                de.PostRead();
                if (de.mymodel != null)
                    de.mymodel.PostRead();
                Items.Add(SolutionItem.Create(de));
            }
        }
    }
}