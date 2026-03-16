using System;
using System.Drawing;
using System.Windows.Forms;
using MVARStudio.MvarLibrary;
using System.Linq;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace MVARStudio
{
    public partial class MainForm : Form
    {
        private BlfFile _blf;
        private MapVariant _mv;
        private DataGridView _dgvObjects;
        private PropertyGrid _propGrid;
        private TextBox _txtTitle;
        private TextBox _txtDescription;
        private ToolStrip _toolStrip;

        // Dark Theme Colors
        private static readonly Color ColorBg = Color.FromArgb(11, 13, 17);
        private static readonly Color ColorSurface = Color.FromArgb(21, 25, 33);
        private static readonly Color ColorBorder = Color.FromArgb(42, 49, 61);
        private static readonly Color ColorAccent = Color.FromArgb(108, 142, 245);
        private static readonly Color ColorText = Color.FromArgb(226, 232, 240);
        private static readonly Color ColorTextMuted = Color.FromArgb(148, 163, 184);

        public MainForm()
        {
            InitializeComponent();
            this.Load += (s, e) => {
                SetupUI();
                ApplyDarkTheme(this);
            };
        }

        private void SetupUI()
        {
            this.Text = "MVAR Studio - Halo Reach Forge Editor";
            this.Size = new Size(1100, 750);
            this.BackColor = ColorBg;

            _toolStrip = new ToolStrip { BackColor = ColorSurface, ForeColor = ColorText, GripStyle = ToolStripGripStyle.Hidden, Padding = new Padding(5) };
            var btnOpen = new ToolStripButton("  Open MVAR  ", null, (s, e) => OpenMvar()) { DisplayStyle = ToolStripItemDisplayStyle.Text, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            var btnSave = new ToolStripButton("  Save MVAR  ", null, (s, e) => SaveMvar()) { DisplayStyle = ToolStripItemDisplayStyle.Text, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            var btnExport = new ToolStripButton("  Export JSON  ", null, (s, e) => ExportToJson()) { DisplayStyle = ToolStripItemDisplayStyle.Text, Font = new Font("Segoe UI", 9) };
            var btnMapSettings = new ToolStripButton("  Map Settings  ", null, (s, e) => {
                if (_mv != null) _propGrid.SelectedObject = new MapProxy(_mv);
                else MessageBox.Show("Please open an MVAR file first.");
            }) { DisplayStyle = ToolStripItemDisplayStyle.Text, Font = new Font("Segoe UI", 9) };
            
            _toolStrip.Items.Add(btnOpen);
            _toolStrip.Items.Add(new ToolStripSeparator());
            _toolStrip.Items.Add(btnSave);
            _toolStrip.Items.Add(btnExport);
            _toolStrip.Items.Add(new ToolStripSeparator());
            _toolStrip.Items.Add(btnMapSettings);
            this.Controls.Add(_toolStrip);

            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 100, Padding = new Padding(15), BackColor = ColorSurface };
            pnlHeader.Controls.Add(new Label { Text = "MAP TITLE", Location = new Point(15, 12), ForeColor = ColorAccent, Font = new Font("Segoe UI", 8, FontStyle.Bold), AutoSize = true });
            _txtTitle = new TextBox { Location = new Point(15, 30), Width = 400, BackColor = ColorBg, ForeColor = ColorText, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10) };
            pnlHeader.Controls.Add(_txtTitle);

            pnlHeader.Controls.Add(new Label { Text = "DESCRIPTION", Location = new Point(440, 12), ForeColor = ColorAccent, Font = new Font("Segoe UI", 8, FontStyle.Bold), AutoSize = true });
            _txtDescription = new TextBox { Location = new Point(440, 30), Width = 600, BackColor = ColorBg, ForeColor = ColorText, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10) };
            pnlHeader.Controls.Add(_txtDescription);
            this.Controls.Add(pnlHeader);
            pnlHeader.BringToFront();

            var split = new SplitContainer { 
                Dock = DockStyle.Fill, 
                Orientation = Orientation.Vertical, 
                BackColor = ColorBorder
            };
            this.Controls.Add(split);
            split.BringToFront();
            
            // Set distance now that we are in the Load event and the form has size
            try { split.SplitterDistance = Math.Max(100, split.Width - 350); } catch {}

            _dgvObjects = new DataGridView { 
                Dock = DockStyle.Fill, 
                AutoGenerateColumns = false, 
                AllowUserToAddRows = false, 
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, 
                MultiSelect = false,
                BackgroundColor = ColorBg,
                ForeColor = ColorText,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false,
                GridColor = ColorBorder
            };
            _dgvObjects.ColumnHeadersDefaultCellStyle.BackColor = ColorSurface;
            _dgvObjects.ColumnHeadersDefaultCellStyle.ForeColor = ColorText;
            _dgvObjects.ColumnHeadersDefaultCellStyle.SelectionBackColor = ColorSurface;
            _dgvObjects.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            _dgvObjects.ColumnHeadersHeight = 35;
            _dgvObjects.DefaultCellStyle.BackColor = ColorBg;
            _dgvObjects.DefaultCellStyle.SelectionBackColor = ColorAccent;
            _dgvObjects.DefaultCellStyle.SelectionForeColor = ColorBg;

            _dgvObjects.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Slot", HeaderText = "Slot", Width = 50 });
            _dgvObjects.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TypeDisplayName", HeaderText = "Type", Width = 180 });
            _dgvObjects.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "TeamDisplayName", HeaderText = "Team", Width = 100 });
            _dgvObjects.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PositionString", HeaderText = "Position", Width = 250, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            
            _dgvObjects.SelectionChanged += (s, e) => {
                if (_dgvObjects.SelectedRows.Count > 0) {
                    var proxy = (ObjectProxy)_dgvObjects.SelectedRows[0].DataBoundItem;
                    _propGrid.SelectedObject = proxy;
                }
            };
            split.Panel1.Controls.Add(_dgvObjects);

            _propGrid = new PropertyGrid { 
                Dock = DockStyle.Fill, 
                ViewBackColor = ColorSurface, 
                ViewForeColor = ColorText, 
                LineColor = ColorBorder,
                CategoryForeColor = ColorAccent,
                HelpBackColor = ColorSurface,
                HelpForeColor = ColorText,
                CommandsBackColor = ColorSurface,
                CommandsForeColor = ColorAccent
            };
            
            // Add padding/margin around the property grid
            var pnlPropWrapper = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 0, 10, 10), BackColor = ColorSurface };
            pnlPropWrapper.Controls.Add(_propGrid);
            split.Panel2.Controls.Add(pnlPropWrapper);
        }

        private void ApplyDarkTheme(Control container)
        {
            foreach (Control c in container.Controls)
            {
                if (c is Label) c.ForeColor = ColorText;
                else if (c is Button) { c.BackColor = ColorBorder; c.ForeColor = ColorText; }
                else if (c is TextBox) { c.BackColor = ColorBg; c.ForeColor = ColorText; }
                
                if (c.HasChildren) ApplyDarkTheme(c);
            }
        }

        private void OpenMvar()
        {
            var ofd = new OpenFileDialog { Filter = "MVAR files (*.mvar)|*.mvar|All files (*.*)|*.*" };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    _blf = BlfFile.Read(ofd.FileName);
                    var mvarChunk = _blf.Chunks.FirstOrDefault(c => c.Magic == "mvar");
                    if (mvarChunk == null) { MessageBox.Show("No mvar chunk found."); return; }

                    _mv = MapVariant.Parse(mvarChunk.Payload);
                    _txtTitle.Text = _mv.Header.Title;
                    _txtDescription.Text = _mv.Header.Description;

                    RefreshGrid();
                }
                catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
            }
        }

        private void RefreshGrid()
        {
            if (_mv == null) return;
            var proxies = _mv.Objects.Where(o => o.Present).Select(o => new ObjectProxy(o, _mv)).ToList();
            _dgvObjects.DataSource = proxies;
        }

        private void SaveMvar()
        {
            if (_mv == null) return;
            var sfd = new SaveFileDialog { Filter = "MVAR files (*.mvar)|*.mvar", FileName = "edited.mvar" };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                _mv.Header.Title = _txtTitle.Text;
                _mv.Header.Description = _txtDescription.Text;
                var mvarChunk = _blf.Chunks.FirstOrDefault(c => c.Magic == "mvar");
                mvarChunk.Payload = _mv.Encode();
                _blf.Save(sfd.FileName);
                MessageBox.Show("Saved successfully!");
            }
        }

        private void ExportToJson()
        {
            if (_mv == null) return;
            var sfd = new SaveFileDialog { Filter = "JSON files (*.json)|*.json", FileName = "map_export.json" };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var options = new JsonSerializerOptions { 
                        WriteIndented = true,
                        ReferenceHandler = ReferenceHandler.IgnoreCycles
                    };
                    var data = new {
                        Title = _mv.Header.Title,
                        Description = _mv.Header.Description,
                        BaseMapId = _mv.MapId,
                        VariantId = _mv.Header.MapId,
                        Author = _mv.Header.CreatedBy.Name,
                        _mv.BudgetMax,
                        _mv.BudgetSpent,
                        Objects = _mv.Objects.Where(o => o.Present).Select(o => new {
                            Slot = o.Slot,
                            Folder = o.ForgeFolder,
                            FolderItem = o.ForgeFolderItem,
                            Position = new { X = o.Position.X, Y = o.Position.Y, Z = o.Position.Z },
                            Yaw = o.Angle,
                            Properties = new {
                                MpType = o.Extra.MpType,
                                SpawnSeq = o.Extra.SpawnSeq,
                                RespawnTime = o.Extra.RespawnTime,
                                Team = o.Extra.TeamRaw,
                                Color = o.Extra.Color,
                                Physics = (PhysicsType)(o.Extra.PlacementFlags >> 6),
                                Symmetry = (SymmetryType)((o.Extra.PlacementFlags >> 2) & 3)
                            }
                        })
                    };
                    string json = JsonSerializer.Serialize(data, options);
                    File.WriteAllText(sfd.FileName, json);
                    MessageBox.Show("Exported to JSON successfully!");
                }
                catch (Exception ex) { MessageBox.Show("Failed to export: " + ex.Message); }
            }
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1000, 600);
            this.Text = "MVAR Studio";
        }
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing) { if (disposing && (components != null)) components.Dispose(); base.Dispose(disposing); }
    }

    public enum PhysicsType : byte { Normal = 0, Fixed = 1, Phased = 3 }
    public enum SymmetryType : byte { Never = 0, Symmetric = 1, Asymmetric = 2, Both = 3 }
    public enum ShapeType : byte { None = 0, Sphere = 1, Cylinder = 2, Box = 3 }

    public class ObjectProxy
    {
        private ForgeObject _obj;
        private MapVariant _mv;

        public ObjectProxy(ForgeObject obj, MapVariant mv) { _obj = obj; _mv = mv; }

        [System.ComponentModel.Category("1. Identification")]
        public int Slot => _obj.Slot;
        
        [System.ComponentModel.Category("1. Identification")]
        public string Type => MP_TYPES.ContainsKey(_obj.Extra.MpType) ? MP_TYPES[_obj.Extra.MpType] : $"Type {_obj.Extra.MpType}";

        [System.ComponentModel.Category("1. Identification")]
        public ushort Folder { get => _obj.ForgeFolder; set => _obj.ForgeFolder = value; }

        [System.ComponentModel.Category("1. Identification")]
        public byte FolderItem { get => _obj.ForgeFolderItem; set => _obj.ForgeFolderItem = value; }

        [System.ComponentModel.Category("4. Forge Label")]
        [System.ComponentModel.TypeConverter(typeof(ForgeLabelConverter))]
        public string Label
        {
            get {
                int idx = _obj.Extra.ForgeLabelIdx;
                if (idx == -1) return "None";
                if (idx >= 0 && idx < _mv.ForgeLabels.Strings.Count) return _mv.ForgeLabels.Strings[idx] ?? "";
                return $"#{idx}";
            }
            set {
                if (value == "None") _obj.Extra.ForgeLabelIdx = -1;
                else {
                    int idx = _mv.ForgeLabels.Strings.IndexOf(value);
                    if (idx != -1) _obj.Extra.ForgeLabelIdx = idx;
                }
            }
        }

        public MapVariant GetMap() => _mv;

        [System.ComponentModel.Category("5. Specific Settings")]
        [System.ComponentModel.Description("Used for Weapon objects.")]
        public byte SpareClips { get => _obj.Extra.SpareClips; set => _obj.Extra.SpareClips = value; }
        
        [System.ComponentModel.Category("5. Specific Settings")]
        [System.ComponentModel.Description("Channel for Teleporters.")]
        public byte TeleChannel { get => _obj.Extra.TeleChannel; set => _obj.Extra.TeleChannel = value; }
        
        [System.ComponentModel.Category("5. Specific Settings")]
        public byte TelePassability { get => _obj.Extra.TelePassability; set => _obj.Extra.TelePassability = value; }
        
        [System.ComponentModel.Category("5. Specific Settings")]
        [System.ComponentModel.Description("Index in the Named Location table.")]
        public byte LocationNameIdx { get => _obj.Extra.LocationNameIdx; set => _obj.Extra.LocationNameIdx = value; }

        [System.ComponentModel.Category("2. Transform")]
        public float X { get => _obj.Position.X; set => _obj.Position.X = value; }
        [System.ComponentModel.Category("2. Transform")]
        public float Y { get => _obj.Position.Y; set => _obj.Position.Y = value; }
        [System.ComponentModel.Category("2. Transform")]
        public float Z { get => _obj.Position.Z; set => _obj.Position.Z = value; }

        [System.ComponentModel.Category("2. Transform")]
        [System.ComponentModel.Description("Yaw angle (0-16383).")]
        public ushort Yaw { get => _obj.Angle; set => _obj.Angle = value; }

        [System.ComponentModel.Category("2. Transform")]
        public bool InBounds { get => _obj.Position.InBounds; set => _obj.Position.InBounds = value; }

        [System.ComponentModel.Category("2. Transform")]
        public bool UpIsGlobal { get => _obj.UpIsGlobal; set => _obj.UpIsGlobal = value; }

        [System.ComponentModel.Category("3. Properties")]
        public sbyte SpawnSeq { get => _obj.Extra.SpawnSeq; set => _obj.Extra.SpawnSeq = value; }
        
        [System.ComponentModel.Category("3. Properties")]
        public byte RespawnTime { get => _obj.Extra.RespawnTime; set => _obj.Extra.RespawnTime = value; }

        [System.ComponentModel.Category("3. Properties")]
        public PhysicsType Physics { 
            get => (PhysicsType)(_obj.Extra.PlacementFlags >> 6); 
            set => _obj.Extra.PlacementFlags = (byte)((_obj.Extra.PlacementFlags & 0x3F) | ((byte)value << 6));
        }

        [System.ComponentModel.Category("3. Properties")]
        public SymmetryType Symmetry { 
            get => (SymmetryType)((_obj.Extra.PlacementFlags >> 2) & 3); 
            set => _obj.Extra.PlacementFlags = (byte)((_obj.Extra.PlacementFlags & 0xF3) | ((byte)value << 2));
        }

        [System.ComponentModel.Category("3. Properties")]
        public bool PlacedAtStart { 
            get => (_obj.Extra.PlacementFlags & 2) == 0; 
            set => _obj.Extra.PlacementFlags = (byte)(value ? (_obj.Extra.PlacementFlags & ~2) : (_obj.Extra.PlacementFlags | 2));
        }

        [System.ComponentModel.Category("3. Properties")]
        public bool GameTypeSpecific { 
            get => (_obj.Extra.PlacementFlags & 0x20) != 0; 
            set => _obj.Extra.PlacementFlags = (byte)(value ? (_obj.Extra.PlacementFlags | 0x20) : (_obj.Extra.PlacementFlags & ~0x20));
        }

        [System.ComponentModel.Category("6. Shape")]
        public ShapeType Shape { get => (ShapeType)_obj.Extra.ShapeType; set => _obj.Extra.ShapeType = (byte)value; }
        [System.ComponentModel.Category("6. Shape")]
        public float Radius { get => _obj.Extra.ShapeRadius; set => _obj.Extra.ShapeRadius = value; }
        [System.ComponentModel.Category("6. Shape")]
        public float Length { get => _obj.Extra.ShapeLength; set => _obj.Extra.ShapeLength = value; }
        [System.ComponentModel.Category("6. Shape")]
        public float Top { get => _obj.Extra.ShapeTop; set => _obj.Extra.ShapeTop = value; }
        [System.ComponentModel.Category("6. Shape")]
        public float Bottom { get => _obj.Extra.ShapeBottom; set => _obj.Extra.ShapeBottom = value; }

        [System.ComponentModel.Category("3. Properties")]
        [System.ComponentModel.Description("Select the owner team.")]
        [System.ComponentModel.TypeConverter(typeof(TeamConverter))]
        public string Team { 
            get => TEAMS.ContainsKey(_obj.Extra.TeamRaw - 1) ? TEAMS[_obj.Extra.TeamRaw - 1] : "Neutral";
            set {
                var kv = TEAMS.FirstOrDefault(x => x.Value == value);
                _obj.Extra.TeamRaw = (byte)(kv.Key + 1);
            }
        }

        [System.ComponentModel.Category("3. Properties")]
        [System.ComponentModel.Description("Object color index. 'Team' uses the owner team color.")]
        [System.ComponentModel.TypeConverter(typeof(ColorConverter))]
        public string Color { 
            get => COLORS.ContainsKey(_obj.Extra.Color) ? COLORS[_obj.Extra.Color] : "Team";
            set {
                var kv = COLORS.FirstOrDefault(x => x.Value == value);
                _obj.Extra.Color = kv.Key;
            }
        }

        public string TypeDisplayName => Type;
        public string TeamDisplayName => TEAMS.ContainsKey(_obj.Extra.TeamRaw - 1) ? TEAMS[_obj.Extra.TeamRaw - 1] : $"Team {_obj.Extra.TeamRaw - 1}";
        public string PositionString => $"({_obj.Position.X:F2}, {_obj.Position.Y:F2}, {_obj.Position.Z:F2})";

        private static readonly Dictionary<int, string> COLORS = new Dictionary<int, string> {
            {-1, "Team"}, {0, "White"}, {1, "Black"}, {2, "Red"}, {3, "Blue"}, {4, "Green"}, {5, "Orange"}, {6, "Purple"}, {7, "Gold"}
        };

        private static readonly Dictionary<int, string> MP_TYPES = new Dictionary<int, string> { 
            {0,"Ordinary"}, {1,"Weapon"}, {2,"Grenade"}, {3,"Projectile"}, {4,"Powerup"}, {5,"Equipment"}, {6,"Ammo Pack"},
            {7,"Light Land Vehicle"}, {8,"Heavy Land Vehicle"}, {9,"Flying Vehicle"}, {10,"Turret"}, {11,"Device"},
            {12,"Teleporter (Two-Way)"}, {13,"Teleporter (Sender)"}, {14,"Teleporter (Receiver)"}, {15,"Player Spawn"},
            {16,"Player Respawn Zone"}, {17,"Secondary Objective"}, {18,"Primary Objective"}, {19,"Named Location Area"},
            {20,"Danger Zone"}, {25,"Safe Volume"}, {26,"Kill Volume"}, {27,"Cinematic Camera"}
        };
        private static readonly Dictionary<int, string> TEAMS = new Dictionary<int, string> { 
            {-1,"Neutral"}, {0,"Red"}, {1,"Blue"}, {2,"Green"}, {3,"Orange"}, {4,"Purple"}, {5,"Gold"}, {6,"Brown"}, {7,"Pink"}, {8,"Neutral"}
        };
    }

    public class MapProxy
    {
        private MapVariant _mv;
        public MapProxy(MapVariant mv) { _mv = mv; }

        [System.ComponentModel.Category("1. Identity")]
        public string Title { get => _mv.Header.Title; set => _mv.Header.Title = value; }
        
        [System.ComponentModel.Category("1. Identity")]
        public string Description { get => _mv.Header.Description; set => _mv.Header.Description = value; }

        [System.ComponentModel.Category("1. Identity")]
        public uint BaseMapId => _mv.MapId;

        [System.ComponentModel.Category("1. Identity")]
        public uint VariantId => _mv.Header.MapId;

        [System.ComponentModel.Category("2. Author")]
        public string CreatedBy { get => _mv.Header.CreatedBy.Name; set => _mv.Header.CreatedBy.Name = value; }

        [System.ComponentModel.Category("2. Author")]
        public string ModifiedBy { get => _mv.Header.ModifiedBy.Name; set => _mv.Header.ModifiedBy.Name = value; }

        [System.ComponentModel.Category("3. Budget")]
        public uint MaxBudget => _mv.BudgetMax;
        
        [System.ComponentModel.Category("3. Budget")]
        public uint SpentBudget => _mv.BudgetSpent;

        [System.ComponentModel.Category("4. Flags")]
        public bool HardBoundary { get => _mv.HardBoundary; set => _mv.HardBoundary = value; }

        [System.ComponentModel.Category("4. Flags")]
        public bool IsCinematic { get => _mv.IsCinematic; set => _mv.IsCinematic = value; }

        [System.ComponentModel.Category("4. Flags")]
        public uint MCC_MapId { get => _mv.MapMccId; set => _mv.MapMccId = value; }

        private const string BBOX_DESC = "WARNING: Changing bounding box values can produce highly unexpected results, including map corruption or objects becoming unselectable.";

        [System.ComponentModel.Category("5. Bounding Box")]
        [System.ComponentModel.Description(BBOX_DESC)]
        public float BBox_XMin { get => _mv.BBox.XMin; set => _mv.BBox.XMin = value; }
        [System.ComponentModel.Category("5. Bounding Box")]
        [System.ComponentModel.Description(BBOX_DESC)]
        public float BBox_XMax { get => _mv.BBox.XMax; set => _mv.BBox.XMax = value; }
        [System.ComponentModel.Category("5. Bounding Box")]
        [System.ComponentModel.Description(BBOX_DESC)]
        public float BBox_YMin { get => _mv.BBox.YMin; set => _mv.BBox.YMin = value; }
        [System.ComponentModel.Category("5. Bounding Box")]
        [System.ComponentModel.Description(BBOX_DESC)]
        public float BBox_YMax { get => _mv.BBox.YMax; set => _mv.BBox.YMax = value; }
        [System.ComponentModel.Category("5. Bounding Box")]
        [System.ComponentModel.Description(BBOX_DESC)]
        public float BBox_ZMin { get => _mv.BBox.ZMin; set => _mv.BBox.ZMin = value; }
        [System.ComponentModel.Category("5. Bounding Box")]
        [System.ComponentModel.Description(BBOX_DESC)]
        public float BBox_ZMax { get => _mv.BBox.ZMax; set => _mv.BBox.ZMax = value; }
    }

    public class ColorConverter : System.ComponentModel.StringConverter
    {
        public override bool GetStandardValuesSupported(System.ComponentModel.ITypeDescriptorContext context) => true;
        public override bool GetStandardValuesExclusive(System.ComponentModel.ITypeDescriptorContext context) => true;
        public override StandardValuesCollection GetStandardValues(System.ComponentModel.ITypeDescriptorContext context)
        {
            return new StandardValuesCollection(new[] { "Team", "White", "Black", "Red", "Blue", "Green", "Orange", "Purple", "Gold" });
        }
    }

    public class TeamConverter : System.ComponentModel.StringConverter
    {
        public override bool GetStandardValuesSupported(System.ComponentModel.ITypeDescriptorContext context) => true;
        public override bool GetStandardValuesExclusive(System.ComponentModel.ITypeDescriptorContext context) => true;
        public override StandardValuesCollection GetStandardValues(System.ComponentModel.ITypeDescriptorContext context)
        {
            return new StandardValuesCollection(new[] { "Neutral", "Red", "Blue", "Green", "Orange", "Purple", "Gold", "Brown", "Pink" });
        }
    }

    public class ForgeLabelConverter : System.ComponentModel.StringConverter
    {
        public override bool GetStandardValuesSupported(System.ComponentModel.ITypeDescriptorContext context) => true;
        public override bool GetStandardValuesExclusive(System.ComponentModel.ITypeDescriptorContext context) => true;
        public override StandardValuesCollection GetStandardValues(System.ComponentModel.ITypeDescriptorContext context)
        {
            var list = new List<string> { "None" };
            if (context.Instance is ObjectProxy proxy)
            {
                var mv = proxy.GetMap();
                if (mv != null) list.AddRange(mv.ForgeLabels.Strings.Where(s => s != null));
            }
            return new StandardValuesCollection(list);
        }
    }
}
