﻿using System;
using System.Text;
using System.Windows.Forms;
using System.Xml.Serialization;
using System.IO;
using System.Collections.Generic;
using System.Drawing;

namespace StickyNote
{
    /// <summary>
    /// メインフォームクラス
    /// </summary>
    public partial class MainForm : Form
    {
        string xmlName;
        string rtfDir = @".\rtf\note";     //rtfを保存するフォルダのパス
        string rtfName;     //rtfファイルの名前

        private List<Form> notes = new List<Form>();
        private LocationComparer comp = new LocationComparer();

        /// <summary>
        /// メインフォームのコンストラクタ
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {   //MainFormロード時
            loadNotes();        //ノートの読み込み
            notes.Sort(comp);   //ノート切替順並び替え
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {   //終了時
            try
            {
                saveNotes();    //ノート保存
            }
            catch
            {   //ノート保存に失敗したら、閉じるのをキャンセルする。
                MessageBox.Show("ノートの保存に失敗しました。");
                e.Cancel = true;
            }
        }

        /// <summary>
        /// ノート新規作成
        /// </summary>
        public void newNote()
        {
            NoteForm noteForm = new NoteForm();
            notes.Add(noteForm);    //ノートのコレクションに追加
            noteForm.Owner = this;  //MainFormを親フォームとして渡す。
            noteForm.Show();
        }

        /// <summary>
        /// ノートの保存
        /// </summary>
        public void saveNotes()
        {
            makeDir();  //ノート保存用ディレクトリの作成

            //既にあるデータをクリア
            fileDel(@".\Notes");
            fileDel(@".\rtf");

            //XmlSerializerオブジェクトを作成
            XmlSerializer serializer = new XmlSerializer(typeof(Settings));

            for (int i = 0; i < notes.Count; i++)
            {   //各NoteFormにループ処理
                //保存用クラスのインスタンス作成
                Settings settings = new Settings();
                NoteForm nf = (NoteForm)notes[i];
                //保存用インスタンスにプロパティセット
                settings.Size = nf.Size;
                settings.Point = nf.Location;
                settings.Color = nf.BackColor.ToArgb();
                settings.Title = nf.title;
                settings.HyperLink = nf.isHyperLink;
                settings.TopMost = nf.isTopMost;
                rtfName = rtfDir + i + ".rtf";
                settings.RtfName = rtfName;
                nf.saveRtf(rtfName);

                //ファイル名の作成
                xmlName = @".\Notes\Note" + i + ".config";
                //書き込むファイルを開く
                StreamWriter sw = new StreamWriter(xmlName, false, new UTF8Encoding(false));
                //シリアル化し、XMLファイルに保存する
                serializer.Serialize(sw, settings);
                sw.Close();
            }
        }

        /// <summary>
        /// ノートの読み込み
        /// </summary>
        public void loadNotes()
        {
            //フォルダ作成
            makeDir();

            //ノートを読込
            IEnumerable<string> files = Directory.EnumerateFiles(@".\Notes");
            XmlSerializer serializer = new XmlSerializer(typeof(Settings));

            foreach (string file in files)
            {
                try
                {
                    StreamReader sr = new StreamReader(file, new UTF8Encoding(false));
                    Settings settings = (Settings)serializer.Deserialize(sr);
                    sr.Close();
                    //ノートを作ってプロパティをセット
                    NoteForm nf = new NoteForm();
                    notes.Add(nf);
                    nf.Size = settings.Size;
                    nf.Location = settings.Point;
                    Color color = Color.FromArgb(settings.Color);
                    nf.BackColor = color;
                    nf.title = settings.Title;
                    nf.isHyperLink = settings.HyperLink;
                    nf.isTopMost = settings.TopMost;
                    nf.loadRtf(settings.RtfName);

                    nf.StartPosition = FormStartPosition.Manual;
                    nf.Owner = this;
                    nf.Show();

                    settings = null;
                }
                catch
                {
                    MessageBox.Show("ノートの読み込みに失敗しました。");
                }
            }
            if (Application.OpenForms.Count == 1)
            {
                newNote();
            }
        }

        private void makeDir()
        {   //フォルダがなければ作る
            if (!Directory.Exists(@".\Notes"))
            {
                Directory.CreateDirectory(@".\Notes");
            }
            if (!Directory.Exists(@".\rtf"))
            {
                Directory.CreateDirectory(@".\rtf");
            }
        }

        private void fileDel(string path)
        {
            DirectoryInfo target = new DirectoryInfo(path);
            //ファイル消す
            foreach (FileInfo file in target.GetFiles())
            {
                file.Delete();
            }
        }

        /// <summary>
        /// ノートを閉じる
        /// </summary>
        /// <param name="note">閉じるノート</param>
        public void noteClose(Form note)
        {
            notes.Remove(note);
            note.Close();
            noteCheck();
        }

        private void noteCheck()
        {   //ノートが一つもなければアプリ終了
            if (Application.OpenForms.Count == 1)
            {
                Application.Exit();
            }
        }

        /// <summary>
        /// アクティブなノートの切り替え
        /// </summary>
        /// <param name="go">次のノート=true 前のノート=false</param>
        public void changeActiveNote(bool go)
        {
            Form targetNote;    //切り替え先のノート

            notes.Sort(comp);

            int noteId = checkNoteId();

            if (go)
            {   //go=trueなら
                if ((noteId + 1) >= notes.Count)
                {   //ノートの数がオーバーしていたら一つ目のノート
                    targetNote = notes[0];
                }
                else
                {   //オーバーしていなければ次のノート
                    targetNote = notes[noteId + 1];
                }
            }
            else
            {   //go=falseなら
                if ((noteId - 1) < 0)
                {   //ノートのidが０未満なら最後のノート
                    targetNote = notes[notes.Count - 1];
                }
                else
                {   //1以上なら前のノート
                    targetNote = notes[noteId - 1];
                }
            }
            //targetNoteをアクティブにする
            targetNote.Activate();
        }

        //アクティブなノートの、openForms上のIDを調べる
        private int checkNoteId()
        {   //openFormsをループして、nowNoteと等しいものを見つける。

            for (int i = 0; i < notes.Count; i++)
            {
                if (Equals(notes[i], ActiveForm))
                {
                    return i;
                }
            }
            return -1;
        }


        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            notes[0].Activate();
        }
    }
}