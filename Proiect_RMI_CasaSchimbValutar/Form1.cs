﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using Tulpep.NotificationWindow;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
//Casa de schimb valutar -  clase: Valute Tranzactii CursuriValutare
namespace Proiect_RMI_CasaSchimbValutar
{
    public partial class Form1 : Form
    {
        private Tranzactie t = new Tranzactie();
        private List<Tranzactie> ListaTranzactii = new List<Tranzactie>();
        private CursValutar cv = new CursValutar();
        private Font font;
        private PrintDocument pd = new PrintDocument();

        Graphics gr;
        Bitmap bmp;
        public Form1()
        {
            InitializeComponent();
            bmp = new Bitmap(pnGraficCursValutar.Width, pnGraficCursValutar.Height);
            gr=Graphics.FromImage(bmp);
            font = new Font("Times New Roman", 8, FontStyle.Bold, GraphicsUnit.Pixel);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //String URLString = "https://www.bnr.ro/nbrfxrates.xml";
            //XmlTextReader reader = new XmlTextReader(URLString);
            //int id = 0;
            //incarcare fisier XML default
            initializareXML();
            //MessageBox.Show("A fost incarcat cursul valutar default.");
            notificare("A fost incarcat cursul valutar default.");
            restaurareBinara();

            //incarcare comboBox
            foreach (Valuta val in cv.Vector_NumeValuta)
            {
                cbMonedaDorita.Items.Add(val.Denumire_scurta);
                cbMonedaOferita.Items.Add(val.Denumire_scurta);
            }
        }

        private void Form1_Close(object sender, EventArgs e)
        {
            salvareBinara(0);
        }

        private void pnGraficCursValutar_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawImage(bmp, 0, 0);
        }

        private void desenareGrafic(String monedaDeRef,int pozMonedaDeRef)
        {
            if (pozMonedaDeRef >= 0)
            {
                float[] cursValutarReferinta = new float[cv.Dimensiune];
                float valoareMonedaRef = 0.0f;
                for (int i = 0; i < cv.Dimensiune; i++)
                {
                    if (!(cv.Vector_NumeValuta[i].Denumire_scurta.Equals(monedaDeRef)))
                    {
                        cursValutarReferinta[i] = cv.Vector_CursValutar[i];
                    }
                    else
                    {
                        cursValutarReferinta[i] = cv.Vector_CursValutar[i];
                        valoareMonedaRef = cv.Vector_CursValutar[i];
                    }
                }
                for (int i = 0; i < cv.Dimensiune - 1; i++)
                {
                    cursValutarReferinta[i] = cursValutarReferinta[i] / valoareMonedaRef;
                }

                Brush br = new SolidBrush(Color.AntiqueWhite);
                Rectangle rec = new Rectangle(10, 10, pnGraficCursValutar.Width - 20, pnGraficCursValutar.Height - 20);
                gr.FillRectangle(br, rec);

                desenareDreptunghiuri(cursValutarReferinta, monedaDeRef,pozMonedaDeRef);

                pnGraficCursValutar.Invalidate();
            }
        }

        private void desenareDreptunghiuri(float[] cursValutarRef,string monedaDeRef,int pozMonedaDeRef)
        {
            int decalajX1 = 0;
            int decalajX2=(pnGraficCursValutar.Width-25)/((cursValutarRef.Length-1)*4)*2;
            //decalajX1 = 15-decalajX2;
            int decalajY1 = 15;
            int decalajY2 = 0;//rezerva 40 de pixeli pentru text
            float maxiValuta = cursValutarRef.Max();
            Brush br = new SolidBrush(Color.Red);
            for (int i = 0; i < cursValutarRef.Length; i++)
            {
                if (i != pozMonedaDeRef)
                {
                    float valoare = cursValutarRef[i] / maxiValuta;
                    decalajY1 = (int)(25 + pnGraficCursValutar.Height * valoare*10);
                    decalajY2 = pnGraficCursValutar.Height - decalajY1 - 65;
                    decalajX1 = decalajX1 + decalajX2*2+1;
                    Rectangle rec = new Rectangle(decalajX1, decalajY1, decalajX2, decalajY2);
                    gr.DrawString(((int)(valoare*10000)).ToString(), font, Brushes.Black, new Point(decalajX1,decalajY1-10));
                    gr.FillRectangle(br, rec);
                    gr.DrawString(cv.Vector_NumeValuta[i].Denumire_scurta, font,Brushes.Black,new Point(decalajX1,pnGraficCursValutar.Height - 60));
                    pnGraficCursValutar.Invalidate();
                }
            }
            pnGraficCursValutar.Invalidate();
        }

        private void notificare(string mesaj)
        {
            PopupNotifier popup=new PopupNotifier();
            popup.TitleFont=new Font("Nipera",20);
            popup.ContentFont = new Font("Nipera",12);
            popup.BodyColor = Color.AntiqueWhite;
            popup.TitleColor = Color.Black;
            popup.TitleText = "Notificare";
            popup.ContentText = mesaj;
            popup.Image = Properties.Resources.logo_notificare.ToBitmap();
            popup.Popup();
        }

        private void initializareXML()
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(@"bnr.xml");
                XmlNodeList xnList = xmlDoc.ChildNodes;

                //initializare nume si data curs valutar
                XmlNodeList xnNumeData = xnList[0].ChildNodes;
                string nume = xnNumeData[0]["Publisher"].InnerText;
                string data = xnNumeData[0]["PublishingDate"].InnerText;
                cv.apelGenCod();
                cv.Nume = nume;
                cv.Data = data;


                XmlNodeList xnCurs = (xnNumeData[1].ChildNodes)[2].ChildNodes;
                int i = 0, n = xnCurs.Count + 1;
                float[] vector_cursValutar = new float[n];
                Valuta[] vector_numeValuta = new Valuta[n];
                vector_numeValuta[0] = new Valuta("RON");
                vector_cursValutar[0] = 1.0f;
                i++;
                foreach (XmlNode xn in xnCurs)
                {
                    string numeVal = xn.Attributes["currency"].Value;
                    vector_numeValuta[i] = new Valuta(numeVal);
                    float nr = (float)Convert.ToDouble(xn.InnerText, CultureInfo.InvariantCulture.NumberFormat);
                    vector_cursValutar[i] = nr;
                    i++;
                }
                cv.setVector_CursValutar(vector_cursValutar, n);
                cv.setVector_NumeValuta(vector_numeValuta, n);
                notificare("A fost incarcat noul fisier XML necesar.");
            }
            catch
            {
                notificare("A esuat incarcarea fisierului XML. Va rog sa verificati fisierul.");
            }
        }

        private int SchimbValutar_apel()
        {
            if (cv.Cod == 0)
            {
                notificare("Nu ati incarcat un curs valutar.");
                return -1;
            }
            else
            {
                try
                {
                    string nume = tbNumePrenume.Text;
                    string adresa = tbAdresa.Text;
                    t.Nume = nume;
                    t.Adresa = adresa;
                }
                catch
                {
                    notificare("Nu ati introdus corect numele si adresa.");
                    return -1;
                }
                int poz1 = -1, poz2 = -1;
                for (int i = 0; i < cv.Dimensiune; i++)
                {
                    if (cv.Vector_NumeValuta[i].Denumire_scurta.Equals(cbMonedaOferita.GetItemText(cbMonedaOferita.SelectedItem)))
                    {
                        poz1 = i;
                    }
                    if (cv.Vector_NumeValuta[i].Denumire_scurta.Equals(cbMonedaDorita.GetItemText(cbMonedaDorita.SelectedItem)))
                    {
                        poz2 = i;
                    }
                }
                if (poz1 == -1 || poz2 == -1)
                {
                    notificare("Valorile alese nu se regasesc in fisierul incarcat.");
                    return poz1;
                }
                else
                {
                    tbMonedaOferitaVal.Text = Convert.ToString((float)(cv.Vector_CursValutar[poz2] / cv.Vector_CursValutar[poz1]));
                    tbMonedaDoritaVal.Text = "1";
                    Valuta[] de_transmis = new Valuta[2];
                    de_transmis[0] = new Valuta(cv.Vector_NumeValuta[poz1].Id, cv.Vector_NumeValuta[poz1].Denumire_scurta);
                    de_transmis[1] = new Valuta(cv.Vector_NumeValuta[poz2].Id, cv.Vector_NumeValuta[poz2].Denumire_scurta);
                    float[] de_transmis_cursValutar = new float[2];
                    de_transmis_cursValutar[0] = cv.Vector_CursValutar[poz1];
                    de_transmis_cursValutar[1] = cv.Vector_CursValutar[poz2];
                    t.CursValutarCurent = new CursValutar(cv.Cod, de_transmis_cursValutar, de_transmis, 2, cv.Data, cv.Nume);
                    return poz1;
                }
            }
        }

        private void adaugareLista()
        {
            foreach(ListViewItem itm in lvTranzactii.Items)
            {
                itm.Remove();
            }
            for(int i=0 ; i< ListaTranzactii.Count; i++)
            {
                ListViewItem item = new ListViewItem(ListaTranzactii[i].Nume);
                item.SubItems.Add(ListaTranzactii[i].Adresa);
                item.SubItems.Add(ListaTranzactii[i].ToString().Split(',')[0]);
                item.SubItems.Add(ListaTranzactii[i].ListaSchimbCantitate[0].ToString());
                item.SubItems.Add(ListaTranzactii[i].ToString().Split(',')[1]);
                item.SubItems.Add(ListaTranzactii[i].ListaSchimbCantitate[1].ToString());
                lvTranzactii.Items.Add(item);
            }
        }

        private void golireFormular()
        {
            tbNumePrenume.Text = string.Empty;
            tbAdresa.Text = string.Empty;
            cbMonedaDorita.Text = string.Empty;
            cbMonedaOferita.Text = string.Empty;
            tbMonedaOferitaVal.Text = string.Empty;
            tbMonedaDoritaVal.Text = string.Empty;
            tbMonedaOferitaFinal.Text = string.Empty;
            tbMonedaDoritaFinal.Text = string.Empty;
        }

        private void salvareBinara(int optiune)
        {
            FileStream fs = new FileStream("fisierulTranzactii.dat", FileMode.OpenOrCreate, FileAccess.Write);
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(fs, ListaTranzactii);
            fs.Close();
            if (optiune == 0)
            {
                MessageBox.Show("S-a salvat fisierul binar cu toate tranzactiile efectuate!");
            }
        }

        private void restaurareBinara()
        {
            try
            {
                FileStream fs = new FileStream("fisierulTranzactii.dat", FileMode.Open, FileAccess.Read);
                BinaryFormatter bf=new BinaryFormatter();
                ListaTranzactii.Clear();
                ListaTranzactii = (List<Tranzactie>)bf.Deserialize(fs);
                fs.Close();
                adaugareLista();
            }
            catch
            {
                notificare("Nu exista salvari locale de tranzactii.");
            }
        }

        private void xMLCursValutarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            initializareXML();
        }

        private void btnSchimbValutar_Click(object sender, EventArgs e)
        {
            desenareGrafic(cbMonedaOferita.Text, SchimbValutar_apel());
        }
        private void btnCalculSchimbValutar_Click(object sender, EventArgs e)
        {
            try
            {
                SchimbValutar_apel();
                t.setCantitatePrimaMoneda((float)Convert.ToDouble(tbMonedaOferitaFinal.Text));
                t.calculSchimbValutarCantitate();
                tbMonedaDoritaFinal.Text = Convert.ToString(t.ListaSchimbCantitate[1]);
            }
            catch
            {
                notificare("Nu a fost oferita o suma pentru schimb.");
            }
        }

        private void btnPrintare_Click(object sender, EventArgs e)
        {
            int n = 0;
            foreach(Tranzactie tran in ListaTranzactii)
            {
                if(tran.Cod_tranzactie==t.Cod_tranzactie)
                {
                    n = 1;
                }
            }
            if(n == 0)
            {
                ListaTranzactii.Add(t);
                t=new Tranzactie();
                golireFormular();
                adaugareLista();
            }
            golireFormular();
        }

        private void sAVEASBINARYFILEToolStripMenuItem_Click(object sender, EventArgs e)
        {
            salvareBinara(0);
        }

        private void tranzactieToolStripMenuItem_Click(object sender, EventArgs e)
        {
            restaurareBinara();
        }

        private void sAVEASToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                StreamWriter sw = new StreamWriter(saveFileDialog1.FileName);
                string mesaj = "FORMAT:Nume&Prenume,Adresa,Oferta,CantitateOferta,Cererea,CentitateCererea";
                mesaj = "";
                foreach (Tranzactie s in ListaTranzactii)
                {
                    mesaj = s.Nume + "," + s.Adresa + "," + s.ToString().Split(',')[0].ToString() + "," + s.ListaSchimbCantitate[0].ToString() + "," + s.ToString().Split(',')[1] + "," + s.ListaSchimbCantitate[1].ToString();
                    sw.WriteLine(mesaj);
                    mesaj = "";
                }
                sw.Close();
                ListaTranzactii.Clear();
            }
        }

        private void tranzactiiTextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                StreamReader sr = new StreamReader(openFileDialog1.FileName);
                //tbStudenti.Text = sr.ReadToEnd();
                sr.Close();
            }
        }

        private void btnPrintare_Click_1(object sender, EventArgs e)
        {
            //printPreviewDialog1.Document = pd;
            //pd.PrintPage += new PrintPageEventHandler(this.pd_print());
            //PrintPreviewDialog dlg=new PrintPreviewDialog();
            //dlg.Document = pd;
            //dlg.ShowDialog();

            System.Windows.Forms.ListView.CheckedListViewItemCollection checkedItems =
            lvTranzactii.CheckedItems;

            int nr_elemente_Selectate = checkedItems.Count;
            if (nr_elemente_Selectate == 1)
            {
                int poz = ListaTranzactii.Count - 1 - lvTranzactii.Items.IndexOf(lvTranzactii.CheckedItems[0]);
                string theDate = dateTimePicker1.Value.ToString("dd-MMM-yyyy");
                Form3 form3 = new Form3(ListaTranzactii[poz], cv, theDate);
                form3.Show();
                lvTranzactii.CheckedItems[0].Checked = false;
            }
            else
            {
                notificare("Trebuie selectata numai o tranzactie pentru printare drept PDF.");
            }
        }

        private void hELPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form2 form2 = new Form2();
            form2.Show();
        }

        private void quickSaveAsPDFToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.ListView.CheckedListViewItemCollection checkedItems =
            lvTranzactii.CheckedItems;

            int nr_elemente_Selectate = checkedItems.Count;
            if(nr_elemente_Selectate == 1)
            {
                int poz = ListaTranzactii.Count-1-lvTranzactii.Items.IndexOf(lvTranzactii.CheckedItems[0]);
                string theDate = dateTimePicker1.Value.ToString("dd-MMM-yyyy");
                Form3 form3 = new Form3(ListaTranzactii[poz], cv, theDate);
                form3.Show();
                lvTranzactii.CheckedItems[0].Checked = false;
            }
            else
            {
                notificare("Trebuie selectata numai o tranzactie pentru printare drept PDF.");
            }
        }

        private void eXPORTPDFToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.ListView.CheckedListViewItemCollection checkedItems =
            lvTranzactii.CheckedItems;

            int nr_elemente_Selectate = checkedItems.Count;
            if (nr_elemente_Selectate == 1)
            {
                int poz = ListaTranzactii.Count - 1 - lvTranzactii.Items.IndexOf(lvTranzactii.CheckedItems[0]);
                string theDate = dateTimePicker1.Value.ToString("dd-MMM-yyyy");
                Form3 form3 = new Form3(ListaTranzactii[poz], cv,theDate);
                form3.Show();
                lvTranzactii.CheckedItems[0].Checked = false;
            }
            else
            {
                notificare("Trebuie selectata numai o tranzactie pentru printare drept PDF.");
            }
        }

        private void hELPToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Form2 form2 = new Form2();
            form2.Show();
        }

        private void eXITAPPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            salvareBinara(0);
            this.Close();
        }

        private void stergereTranzactieToolStripMenuItem_Click(object sender, EventArgs e)
        {

            System.Windows.Forms.ListView.CheckedListViewItemCollection checkedItems =
            lvTranzactii.CheckedItems;

            if(checkedItems.Count > 0)
            {
                salvareBinara(1);
                for (int i = 0; i < checkedItems.Count; i++)
                {
                    int poz = ListaTranzactii.Count- lvTranzactii.Items.IndexOf(lvTranzactii.CheckedItems[i]);
                    if (ListaTranzactii.Count == 1)
                    {
                        ListaTranzactii.Clear();
                    }
                    else
                    {
                        ListaTranzactii.RemoveAt(poz - 1);
                    }
                }
                foreach (ListViewItem itm in lvTranzactii.Items)
                {
                    itm.Remove();
                }
                for (int i = 0; i < ListaTranzactii.Count; i++)
                {
                    ListViewItem item = new ListViewItem(ListaTranzactii[i].Nume);
                    item.SubItems.Add(ListaTranzactii[i].Adresa);
                    item.SubItems.Add(ListaTranzactii[i].ToString().Split(',')[0]);
                    item.SubItems.Add(ListaTranzactii[i].ListaSchimbCantitate[0].ToString());
                    item.SubItems.Add(ListaTranzactii[i].ToString().Split(',')[1]);
                    item.SubItems.Add(ListaTranzactii[i].ListaSchimbCantitate[1].ToString());
                    lvTranzactii.Items.Add(item);
                }
            }
            else
            {
                notificare("Selectati o tranzactie pentru stergere.");
            }
        }

        private void restaurareTranzactiiToolStripMenuItem_Click(object sender, EventArgs e)
        {
            restaurareBinara();
        }

        private void hELPToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            Form2 form2 = new Form2();
            form2.Show();
        }
    }
}
