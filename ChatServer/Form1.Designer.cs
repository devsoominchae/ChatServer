namespace ChatServer
{
    partial class ChatServerForm
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
            ServerStartButton = new Button();
            ServerStopButton = new Button();
            OpenChatLabel = new Label();
            splitContainer1 = new SplitContainer();
            OpenChatTextBox = new TextBox();
            IPAddressTextBox = new TextBox();
            PortTextBox = new TextBox();
            IPAddressLabel = new Label();
            PortLabel = new Label();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            SuspendLayout();
            // 
            // ServerStartButton
            // 
            ServerStartButton.Dock = DockStyle.Bottom;
            ServerStartButton.Location = new Point(0, 2);
            ServerStartButton.Name = "ServerStartButton";
            ServerStartButton.Size = new Size(205, 34);
            ServerStartButton.TabIndex = 0;
            ServerStartButton.Text = "Start";
            ServerStartButton.UseVisualStyleBackColor = true;
            ServerStartButton.Click += ServerStartButton_Click;
            // 
            // ServerStopButton
            // 
            ServerStopButton.Dock = DockStyle.Bottom;
            ServerStopButton.Location = new Point(0, 2);
            ServerStopButton.Name = "ServerStopButton";
            ServerStopButton.Size = new Size(201, 34);
            ServerStopButton.TabIndex = 1;
            ServerStopButton.Text = "Stop";
            ServerStopButton.UseVisualStyleBackColor = true;
            ServerStopButton.Click += ServerStopButton_Click;
            // 
            // OpenChatLabel
            // 
            OpenChatLabel.AutoSize = true;
            OpenChatLabel.Location = new Point(30, 20);
            OpenChatLabel.Name = "OpenChatLabel";
            OpenChatLabel.Size = new Size(99, 25);
            OpenChatLabel.TabIndex = 2;
            OpenChatLabel.Text = "Open Chat";
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Bottom;
            splitContainer1.Location = new Point(0, 541);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(ServerStartButton);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(ServerStopButton);
            splitContainer1.Size = new Size(410, 36);
            splitContainer1.SplitterDistance = 205;
            splitContainer1.TabIndex = 3;
            // 
            // OpenChatTextBox
            // 
            OpenChatTextBox.Location = new Point(30, 48);
            OpenChatTextBox.Multiline = true;
            OpenChatTextBox.Name = "OpenChatTextBox";
            OpenChatTextBox.ReadOnly = true;
            OpenChatTextBox.ScrollBars = ScrollBars.Vertical;
            OpenChatTextBox.Size = new Size(351, 400);
            OpenChatTextBox.TabIndex = 4;
            // 
            // IPAddressTextBox
            // 
            IPAddressTextBox.Location = new Point(0, 506);
            IPAddressTextBox.Name = "IPAddressTextBox";
            IPAddressTextBox.Size = new Size(205, 31);
            IPAddressTextBox.TabIndex = 5;
            // 
            // PortTextBox
            // 
            PortTextBox.Location = new Point(209, 506);
            PortTextBox.Name = "PortTextBox";
            PortTextBox.Size = new Size(201, 31);
            PortTextBox.TabIndex = 6;
            // 
            // IPAddressLabel
            // 
            IPAddressLabel.AutoSize = true;
            IPAddressLabel.Location = new Point(0, 478);
            IPAddressLabel.Name = "IPAddressLabel";
            IPAddressLabel.Size = new Size(99, 25);
            IPAddressLabel.TabIndex = 7;
            IPAddressLabel.Text = "IP Address";
            // 
            // PortLabel
            // 
            PortLabel.AutoSize = true;
            PortLabel.Location = new Point(209, 478);
            PortLabel.Name = "PortLabel";
            PortLabel.Size = new Size(45, 25);
            PortLabel.TabIndex = 8;
            PortLabel.Text = "Port";
            // 
            // ChatServerForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(410, 577);
            Controls.Add(PortLabel);
            Controls.Add(IPAddressLabel);
            Controls.Add(PortTextBox);
            Controls.Add(IPAddressTextBox);
            Controls.Add(OpenChatTextBox);
            Controls.Add(splitContainer1);
            Controls.Add(OpenChatLabel);
            Name = "ChatServerForm";
            Text = "Chat Server";
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button ServerStartButton;
        private Button ServerStopButton;
        private Label OpenChatLabel;
        private SplitContainer splitContainer1;
        private TextBox OpenChatTextBox;
        private TextBox IPAddressTextBox;
        private TextBox PortTextBox;
        private Label IPAddressLabel;
        private Label PortLabel;
    }
}
