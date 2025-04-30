namespace WinFormsApp1
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            listView1 = new ListView();
            textBox1 = new TextBox();
            button1 = new Button();
            SuspendLayout();
            // 
            // listView1
            // 
            listView1.Location = new Point(47, 151);
            listView1.Name = "listView1";
            listView1.Size = new Size(698, 226);
            listView1.TabIndex = 0;
            listView1.UseCompatibleStateImageBehavior = false;
            listView1.View = View.Details;
            listView1.Columns.Add("Size", 100);
            listView1.Columns.Add("Path", 500);

            // 
            // textBox1
            // 
            textBox1.Location = new Point(47, 84);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(405, 23);
            textBox1.TabIndex = 1;
            textBox1.Text = @"c:\";
            // 
            // button1
            // 
            button1.BackColor = Color.Green;
            button1.Location = new Point(670, 83);
            button1.Name = "button1";
            button1.Size = new Size(75, 23);
            button1.TabIndex = 2;
            button1.Text = "start";
            button1.UseVisualStyleBackColor = false;
            button1.Click += button1_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(button1);
            Controls.Add(textBox1);
            Controls.Add(listView1);
            Name = "Form1";
            Text = "My App";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ListView listView1;
        private TextBox textBox1;
        private Button button1;
    }
}
