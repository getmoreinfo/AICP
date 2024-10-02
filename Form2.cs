using System;
using System.Reflection.Emit;
using System.Windows.Forms;

namespace DU_AICP
{
    public partial class Form2 : MetroFramework.Forms.MetroForm
    {
        public Form2(string artist, string inputBPM, string trackName, string spotifyTrackBPM, string chordifyDetails, string albumCoverUrl)
        {
            InitializeComponent();

            // Label에 정보를 출력
            label1.Text = $"입력한 아티스트: {artist}, 입력한 BPM: {inputBPM}\n" +
                          $"Spotify 트랙: {trackName}, Spotify BPM: {spotifyTrackBPM}\n" +
                          $"Chordify Song Details: \n{chordifyDetails}";

            // PictureBox에 앨범 이미지 표시
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox1.Load(albumCoverUrl);
        }
    }
}