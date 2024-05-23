using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace PracticeMode
{
    public class SoapboxRecorderFrame
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
        public Vector3 angularVelocity;
        public float time;
        public bool isCheckpoint;
    }

    public class SoapboxRecorder
    {
        public List<SoapboxRecorderFrame> frames;
        public int currentFrameIndex = 0;

        public SoapboxRecorder()
        {
            frames = new List<SoapboxRecorderFrame>();
        }

        public void AddFrame(SoapboxRecorderFrame frame)
        {
            frames.Add(frame);
        }

        public void Clear()
        {
            frames.Clear();
            currentFrameIndex = 0;
        }

        public void SetCurrentFrameIndex(int index)
        {
            if (index >= 0 && index < frames.Count)
            {
                currentFrameIndex = index;
            }
        }

        public void RewindToStart()
        {
            currentFrameIndex = 0;
        }

        public void RemoveFramesAfterCurrent()
        {
            if (currentFrameIndex >= 0 && currentFrameIndex < frames.Count)
            {
                frames.RemoveRange(currentFrameIndex + 1, frames.Count - currentFrameIndex - 1);
            }
        }

        public void NextFrame()
        {
            if (currentFrameIndex < frames.Count - 1)
            {
                currentFrameIndex++;
            }
        }

        public void PreviousFrame()
        {
            if (currentFrameIndex > 0)
            {
                currentFrameIndex--;
            }
        }

        public SoapboxRecorderFrame GetCurrentFrame()
        {
            if (currentFrameIndex >= 0 && currentFrameIndex < frames.Count)
            {
                return frames[currentFrameIndex];
            }
            else
            {
                return null;
            }
        }

        public SoapboxRecorderFrame GetFirstFrame()
        {
            if (frames.Count > 0)
            {
                return frames[0];
            }
            else
            {
                return null;
            }
        }

        public SoapboxRecorderFrame GetLastFrame()
        {
            if (frames.Count > 0)
            {
                return frames[frames.Count - 1];
            }
            else
            {
                return null;
            }
        }

        public int GetFrameIndexByTime(float time)
        {
            int frameIndex = frames.FindIndex(frame => frame.time >= time);
            return frameIndex;
        }

        public void NextCheckpointFrame()
        {
            int foundIndex = -1;

            if (currentFrameIndex + 1 >= frames.Count - 1)
            {
                SetCurrentFrameIndex(frames.Count - 1);
            }
            else
            {
                for (int i = currentFrameIndex + 1; i < frames.Count; i++)
                {
                    if (frames[i].isCheckpoint)
                    {
                        foundIndex = i;
                        break;
                    }
                }

                if (foundIndex != -1)
                {
                    SetCurrentFrameIndex(foundIndex);
                }
                else
                {
                    SetCurrentFrameIndex(frames.Count - 1);
                }
            }
        }

        public void PreviousCheckpointFrame()
        {
            int foundIndex = -1;

            if (currentFrameIndex - 1 <= 0)
            {
                SetCurrentFrameIndex(0);
            }
            else
            {
                for (int i = currentFrameIndex - 1; i >= 0; i--)
                {
                    if (frames[i].isCheckpoint)
                    {
                        foundIndex = i;
                        break;
                    }
                }

                if (foundIndex != -1)
                {
                    SetCurrentFrameIndex(foundIndex);
                }
                else
                {
                    SetCurrentFrameIndex(0);
                }
            }
        }
    }
}
