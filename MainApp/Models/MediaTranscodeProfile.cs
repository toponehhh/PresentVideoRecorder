using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.MediaProperties;

namespace PresentVideoRecorder.Models
{
    public class MediaTranscodeProfile : ModelBase
    {
        public string Name { get; set; }
        public string TanscodeFileSaveDirectory { get; set; }
        public VideoEncodingQuality VideoTanscodeQuality { get; set; }
        public AudioEncodingQuality AudioTanscodeQuality { get; set; }
        public CombineAudioWay CombineAudioToVideoWay { get; set; }
    }

    public enum CombineAudioWay
    {
        WithCameraVideo,
        WithDesktopVideo,
        Independent
    }
}
