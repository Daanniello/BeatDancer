using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;

namespace ReplayBattleRoyal
{
    public class AudioManager : MainWindow
    {
        // Sound api functions
        [DllImport("winmm.dll")]
        static extern Int32 mciSendString(string command, StringBuilder buffer, int bufferSize, IntPtr hwndCallback);

        public static async void PlayHitSound()
        {
            var soundPlayerHitsound = new MediaPlayer();
            soundPlayerHitsound.Open(new Uri(AppContext.BaseDirectory + $@"Audio\SoundEffect\HitSound.wav"));
            soundPlayerHitsound.Play();            
        }

        public async static Task PlayHitSounds(List<double> noteTimings)
        {


            var count = 0;
            foreach (var time in noteTimings)
            {
                var soundPlayerHitsound = new MediaPlayer();
                soundPlayerHitsound.Open(new Uri(AppContext.BaseDirectory + $@"Audio\SoundEffect\HitSound.wav"));
                await Task.Delay(TimeSpan.FromMilliseconds((noteTimings[count + 1] - time) * 1000));
                soundPlayerHitsound.Play();

                count++;
            }
        }
      
    }
}
