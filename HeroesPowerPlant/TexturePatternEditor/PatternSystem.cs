﻿using System;
using System.Collections.Generic;
using System.IO;

namespace HeroesPowerPlant.TexturePatternEditor
{
    public class PatternSystem
    {
        public bool UnsavedChanges { get; set; } = false;

        private string currentlyOpenTXC;
        public string CurrentlyOpenTXC
        {
            get => currentlyOpenTXC;
            private set => currentlyOpenTXC = value;
        }

        public List<PatternEntry> patterns;

        public PatternSystem()
        {
            patterns = new List<PatternEntry>();
            UnsavedChanges = false;
        }

        public PatternSystem(string fileName)
        {
            patterns = new List<PatternEntry>();
            currentlyOpenTXC = fileName;

            using var patternReader = new BinaryReader(new FileStream(currentlyOpenTXC, FileMode.Open));

            uint frameCount = patternReader.ReadUInt32();

            while (frameCount != 0xFFFFFFFF)
            {
                patternReader.BaseStream.Position += 0x204;

                string textureName = new string(patternReader.ReadChars(0x20)).Trim('\0');
                string animationName = new string(patternReader.ReadChars(0x20)).Trim('\0');

                List<Frame> frames = new List<Frame>();

                ushort FrameOffset = patternReader.ReadUInt16();
                ushort TextureNumber = patternReader.ReadUInt16();

                while (FrameOffset != 0xFFFF & TextureNumber != 0xFFFF)
                {
                    frames.Add(new Frame()
                    {
                        FrameOffset = FrameOffset,
                        TextureNumber = TextureNumber
                    });

                    FrameOffset = patternReader.ReadUInt16();
                    TextureNumber = patternReader.ReadUInt16();
                }

                patterns.Add(new PatternEntry()
                {
                    FrameCount = frameCount,
                    TextureName = textureName,
                    AnimationName = animationName,
                    frames = frames
                });

                frameCount = patternReader.ReadUInt32();
            }

            UnsavedChanges = false;
        }

        public void Save(string fileName)
        {
            currentlyOpenTXC = fileName;
            Save();
        }

        public void Save()
        {
            using var patternWriter = new BinaryWriter(new FileStream(currentlyOpenTXC, FileMode.Create));

            foreach (PatternEntry p in patterns)
            {
                patternWriter.Write(p.FrameCount);
                patternWriter.BaseStream.Position += 0x204;

                foreach (char c in p.TextureName)
                    patternWriter.Write(c);
                for (int i = p.TextureName.Length; i < 0x20; i++)
                    patternWriter.Write((byte)0);
                foreach (char c in p.AnimationName)
                    patternWriter.Write(c);
                for (int i = p.AnimationName.Length; i < 0x20; i++)
                    patternWriter.Write((byte)0);

                foreach (Frame f in p.frames)
                {
                    patternWriter.Write(f.FrameOffset);
                    patternWriter.Write(f.TextureNumber);
                }

                patternWriter.Write(0xFFFFFFFF);
            }
            patternWriter.Write(0xFFFFFFFF);

            UnsavedChanges = false;
        }

        public IEnumerable<string> GetPatternEntries()
        {
            List<String> list = new List<string>();
            foreach (PatternEntry p in patterns)
                list.Add(p.ToString());
            return list;
        }

        public int GetPatternCount()
        {
            return patterns.Count;
        }

        public PatternEntry GetPatternAt(int index)
        {
            if (index >= 0 & index < patterns.Count)
                return patterns[index];
            throw new IndexOutOfRangeException();
        }

        public string Add()
        {
            PatternEntry p = new PatternEntry();
            patterns.Add(p);
            UnsavedChanges = true;
            return p.ToString();
        }

        public string Add(int index)
        {
            PatternEntry p = new PatternEntry(patterns[index]);
            patterns.Add(p);
            UnsavedChanges = true;
            return p.ToString();
        }

        public int Remove(int index)
        {
            if (index >= 0 & index < patterns.Count)
            {
                patterns[index].StopAnimation(Program.MainForm.LevelEditor.bspRenderer, Program.MainForm.renderer.dffRenderer);
                patterns.RemoveAt(index);
                UnsavedChanges = true;
                return index;
            }
            throw new IndexOutOfRangeException();
        }

        public void Deselect()
        {
            foreach (PatternEntry p in patterns)
                p.isSelected = false;
        }

        // Rendering

        private bool switcher = true;

        public void Animate(TexturePatternEditor editor)
        {
            if (switcher)
                foreach (PatternEntry p in patterns)
                    p.Animate(editor, Program.MainForm.LevelEditor.bspRenderer, Program.MainForm.renderer.dffRenderer);
            switcher = !switcher;
        }

        public void StopAnimation()
        {
            foreach (PatternEntry p in patterns)
                p.StopAnimation(Program.MainForm.LevelEditor.bspRenderer, Program.MainForm.renderer.dffRenderer);
        }
    }
}
