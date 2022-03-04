using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;

namespace ReplayBattleRoyal
{
    public class AudioManager : MainWindow
    {
        private static Uri path = new Uri(GlobalConfig.BasePath + $@"/Resources/SoundEffects/HitSound.wav");

        // Sound api functions
        [DllImport("winmm.dll")]
        static extern Int32 mciSendString(string command, StringBuilder buffer, int bufferSize, IntPtr hwndCallback);

        public static async void PlayHitSound()
        {
            p();
        }

        public static async Task p()
        {
            var soundPlayerHitsound = new MediaPlayer() { Volume = 0.33 };
            soundPlayerHitsound.Open(path);
            soundPlayerHitsound.Play();
            await Task.Delay(300);
            //soundPlayerHitsound.Close();
        }

        public async static Task PlayHitSounds(List<double> noteTimings)
        {
            var queue = new Queue<MediaPlayer>();
            for (var i = 0; i < 20; i++)
            {
                var player = new MediaPlayer();
                player.Open(path);
                queue.Enqueue(player);
            }



            var count = 0;
            await Task.Delay(TimeSpan.FromMilliseconds(noteTimings[0] * 1000));
            foreach (var time in noteTimings)
            {
                var waitTime = (noteTimings[count + 1] - time);
                count++;
                if (waitTime < 0) continue;

                queue.ElementAt(2).Stop();
                var first = queue.First();
                first.Play();
                queue.Enqueue(first);
                queue.Dequeue();

                await Task.Delay(TimeSpan.FromSeconds(waitTime));
            }
        }

    }
}
