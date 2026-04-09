using System;
using System.Collections.Generic;
using System.IO;
using InnoWerks.Computers.Apple;
using InnoWerks.Simulators;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace InnoWerks.Emulators.AppleIIe
{
    public enum ToolbarAction
    {
        None,
        Reset,
        Reboot,
        DiskEject,
        DiskInsert,
    }

    public sealed class ToolbarRenderer : IDisposable
    {
        private readonly GraphicsDevice graphicsDevice;
        private Texture2D whitePixel;
        private Texture2D disk2Off;
        private Texture2D disk2On;
        private Texture2D disk2OffEmpty;
        private Texture2D disk2OnEmpty;
        private Texture2D hardDriveOff;
        private SpriteFont font;

        private readonly List<ToolbarItem> items = [];

        private bool disposed;

        // each toolbar item has a bounding rectangle, action, and context
        private sealed class ToolbarItem
        {
            public Rectangle Bounds { get; set; }
            public ToolbarAction Action { get; set; }
            public string Label { get; set; }
            public Func<Texture2D> GetIcon { get; set; }

            // for disk items
            public DiskIISlotDevice DiskDevice { get; set; }
            public int DriveNumber { get; set; }

            // true for items that are just informational (no click action)
            public bool IsInfoOnly { get; set; }
        }

        public ToolbarRenderer(GraphicsDevice graphicsDevice)
        {
            ArgumentNullException.ThrowIfNull(graphicsDevice);
            this.graphicsDevice = graphicsDevice;
        }

        public void LoadContent(ContentManager contentManager)
        {
            ArgumentNullException.ThrowIfNull(contentManager);

            whitePixel = new Texture2D(graphicsDevice, 1, 1);
            whitePixel.SetData([Color.White]);

            font = contentManager.Load<SpriteFont>("DebugFont");

            disk2Off = LoadPng("Content/Icons/disk2off.png");
            disk2On = LoadPng("Content/Icons/disk2on.png");
            disk2OffEmpty = LoadPng("Content/Icons/disk2off-empty.png");
            disk2OnEmpty = LoadPng("Content/Icons/disk2on-empty.png");

            // use the off-state disk icon as a placeholder for hard drives for now
            hardDriveOff = LoadPng("Content/Icons/disk2off.png");
        }

        public void ConfigureItems(ISlotDevice[] slotDevices)
        {
            items.Clear();

            // add reset and reboot buttons first
            items.Add(new ToolbarItem
            {
                Action = ToolbarAction.Reset,
                Label = "Reset",
            });

            items.Add(new ToolbarItem
            {
                Action = ToolbarAction.Reboot,
                Label = "Reboot",
            });

            // add disk drive items for each DiskII and ProDOS slot
            if (slotDevices != null)
            {
                for (var slot = 0; slot < slotDevices.Length; slot++)
                {
                    if (slotDevices[slot] is DiskIISlotDevice diskDevice)
                    {
                        for (var drive = 0; drive < 2; drive++)
                        {
                            var driveNum = drive;
                            items.Add(new ToolbarItem
                            {
                                // Label = $"S{slot}D{drive + 1}",
                                DiskDevice = diskDevice,
                                DriveNumber = driveNum,
                                GetIcon = () =>
                                {
                                    var d = diskDevice.GetDrive(driveNum);
                                    return (d.HasDisk, diskDevice.IsMotorOn(driveNum)) switch
                                    {
                                        (true, true) => disk2On,
                                        (true, false) => disk2Off,
                                        (false, true) => disk2OnEmpty,
                                        (false, false) => disk2OffEmpty,
                                    };
                                },
                            });
                        }
                    }
                    else if (slotDevices[slot] is ProDOSSlotDevice)
                    {
                        items.Add(new ToolbarItem
                        {
                            Label = $"S{slot}HD",
                            IsInfoOnly = true,
                            GetIcon = () => hardDriveOff,
                        });
                    }
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch, HostLayout hostLayout)
        {
            ArgumentNullException.ThrowIfNull(spriteBatch);
            ArgumentNullException.ThrowIfNull(hostLayout);

            var toolbar = hostLayout.Toolbar;

            // draw toolbar background
            spriteBatch.Draw(whitePixel, toolbar, new Color(40, 40, 40));

            // layout items left to right
            var x = toolbar.X + HostLayout.Padding;
            var y = toolbar.Y;
            var iconSize = toolbar.Height;

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];

                if (item.GetIcon != null)
                {
                    // device icon — scale to fit toolbar height
                    var icon = item.GetIcon();
                    var aspectRatio = (float)icon.Width / icon.Height;
                    var iconWidth = (int)(iconSize * aspectRatio);

                    var itemRect = new Rectangle(x, y, iconWidth, iconSize);
                    item.Bounds = itemRect;

                    spriteBatch.Draw(icon, itemRect, Color.White);

                    // draw label below icon
                    if (string.IsNullOrEmpty(item.Label) == false)
                    {
                        var labelPos = new Vector2(x + 2, y + iconSize - font.LineSpacing);
                        spriteBatch.DrawString(font, item.Label, labelPos, Color.White);
                    }

                    x += iconWidth + HostLayout.Padding;
                }
                else
                {
                    // text button (Reset, Reboot)
                    var textSize = font.MeasureString(item.Label);
                    var buttonWidth = (int)textSize.X + 16;
                    var buttonHeight = (int)textSize.Y + 8;
                    var buttonY = y + (iconSize - buttonHeight) / 2;

                    var buttonRect = new Rectangle(x, buttonY, buttonWidth, buttonHeight);
                    item.Bounds = buttonRect;

                    // button background
                    spriteBatch.Draw(whitePixel, buttonRect, new Color(70, 70, 70));
                    // button border
                    DrawBorder(spriteBatch, buttonRect, new Color(120, 120, 120));

                    // button text centered
                    var textPos = new Vector2(
                        x + (buttonWidth - textSize.X) / 2,
                        buttonY + (buttonHeight - textSize.Y) / 2);
                    spriteBatch.DrawString(font, item.Label, textPos, Color.White);

                    x += buttonWidth + HostLayout.Padding;
                }
            }
        }

        /// <summary>
        /// Tests if a click at the given position hits a toolbar item and returns
        /// the action and context. For disk items, returns DiskEject if a disk is
        /// inserted, DiskInsert if empty.
        /// </summary>
        public (ToolbarAction action, DiskIISlotDevice device, int driveNumber) HandleClick(Point mousePos)
        {
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item.Bounds.Contains(mousePos) && !item.IsInfoOnly)
                {
                    if (item.Action == ToolbarAction.Reset || item.Action == ToolbarAction.Reboot)
                    {
                        return (item.Action, null, 0);
                    }

                    if (item.DiskDevice != null)
                    {
                        var hasDisk = item.DiskDevice.GetDrive(item.DriveNumber).HasDisk;
                        return (hasDisk ? ToolbarAction.DiskEject : ToolbarAction.DiskInsert,
                                item.DiskDevice, item.DriveNumber);
                    }
                }
            }

            return (ToolbarAction.None, null, 0);
        }

        private void DrawBorder(SpriteBatch spriteBatch, Rectangle rect, Color color)
        {
            spriteBatch.Draw(whitePixel, new Rectangle(rect.X, rect.Y, rect.Width, 1), color);
            spriteBatch.Draw(whitePixel, new Rectangle(rect.X, rect.Bottom - 1, rect.Width, 1), color);
            spriteBatch.Draw(whitePixel, new Rectangle(rect.X, rect.Y, 1, rect.Height), color);
            spriteBatch.Draw(whitePixel, new Rectangle(rect.Right - 1, rect.Y, 1, rect.Height), color);
        }

        private Texture2D LoadPng(string path)
        {
            using var stream = File.OpenRead(path);
            return Texture2D.FromStream(graphicsDevice, stream);
        }

        public void Dispose()
        {
            if (!disposed)
            {
                whitePixel?.Dispose();
                disk2Off?.Dispose();
                disk2On?.Dispose();
                disk2OffEmpty?.Dispose();
                disk2OnEmpty?.Dispose();
                hardDriveOff?.Dispose();
                disposed = true;
            }
        }
    }
}
