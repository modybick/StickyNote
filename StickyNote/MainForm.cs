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
        private string rtfDir = @".\rtf\note";     //rtfを保存するフォルダのパス
        private string rtfName;     //rtfファイルの名前
        private string xmlName;     //xmlファイルの名前

        /// <summary>
        /// メインフォームのコンストラクタ
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {   //MainFormロード時
            //ノート読み込み
            loadNotes();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {   //終了時
            //ノート保存
            try
            {
                saveNotes();
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
            noteForm.Owner = this;
            noteForm.Show();
        }

        /// <summary>
        /// ノートの保存
        /// </summary>
        public void saveNotes()
        {
            //フォルダ作成
            makeDir();

            //既にあるデータをクリア
            fileDel(@".\Notes");
            fileDel(@".\rtf");

            //XmlSerializerオブジェクトを作成
            XmlSerializer serializer = new XmlSerializer(typeof(Settings));
            for (int i = 1; i < Application.OpenForms.Count; i++)
            {   //各NoteFormにループ処理
                //保存用クラスのインスタンス作成
                Settings settings = new Settings();
                NoteForm nf = (NoteForm)Application.OpenForms[i];
                //保存用インスタンスにプロパティセット
                settings.Size = nf.Size;
                settings.Point = nf.Location;
                settings.Color = nf.BackColor.ToArgb();
                settings.Title = nf.title;
                settings.HyperLink = nf.isHyperLink;
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
                    nf.Size = settings.Size;
                    nf.Location = settings.Point;
                    Color color = Color.FromArgb(settings.Color);
                    nf.BackColor = color;
                    nf.title = settings.Title;
                    nf.isHyperLink = settings.HyperLink;
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
        public void changeActiveNote(Boolean go)
        {
            Form targetNote;    //切り替え先のノート
            if (go)
            {   //go=trueなら
                if ((checkNoteId(ActiveForm) + 1) > Application.OpenForms.Count - 1)
                {   //ノートの数がオーバーしていたら一つ目のノート
                    targetNote = Application.OpenForms[1];
                }
                else
                {   //オーバーしていなければ次のノート
                    targetNote = Application.OpenForms[checkNoteId(ActiveForm) + 1];
                }
            }
            else
            {   //go=falseなら
                if ((checkNoteId(ActiveForm) - 1) <= 0)
                {   //ノートのidが０以下なら最後のノート
                    targetNote = Application.OpenForms[Application.OpenForms.Count - 1];
                }
                else
                {   //1以上なら前のノート
                    targetNote = Application.OpenForms[checkNoteId(ActiveForm) - 1];
                }
            }
            //targetNoteをアクティブにする
            targetNote.Activate();
        }

        //アクティブなノートの、openForms上のIDを調べる
        private int checkNoteId(Form note)
        {   //openFormsをループして、nowNoteと等しいものを見つける。
            for (int i = 0; i <= Application.OpenForms.Count; i++)
            {
                if (note == Application.OpenForms[i])
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// ノートの印刷を行う。
        /// </summary>
        /// <param name="noteForm">印刷するノート</param>
        public void PrintNote(NoteForm noteForm)
        {   //ノートを印刷する
            //印刷用SuperRichTextBox作成
            SRichTextBoxLibrary.SuperRichTextBox printSrtb =
                new SRichTextBoxLibrary.SuperRichTextBox();

            printSrtb.Text = noteForm.title + "\n\n";
            printSrtb.SelectAll();
            printSrtb.SelectionFont =
                new Font(printSrtb.SelectionFont.FontFamily, 18, printSrtb.SelectionFont.Style);
            printSrtb.SelectionStart = printSrtb.TextLength;
            printSrtb.SelectedRtf = noteForm.sRichTextBox.Rtf;
            printSrtb.print();
            printSrtb.Dispose();
        }

    }
}
