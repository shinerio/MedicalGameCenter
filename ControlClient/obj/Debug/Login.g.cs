﻿#pragma checksum "..\..\Login.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "2033ABA75359F62FA3556B90D49061BA"
//------------------------------------------------------------------------------
// <auto-generated>
//     此代码由工具生成。
//     运行时版本:4.0.30319.42000
//
//     对此文件的更改可能会导致不正确的行为，并且如果
//     重新生成代码，这些更改将会丢失。
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace ControlClient {
    
    
    /// <summary>
    /// Login
    /// </summary>
    public partial class Login : System.Windows.Window, System.Windows.Markup.IComponentConnector {
        
        
        #line 11 "..\..\Login.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Grid topTitle;
        
        #line default
        #line hidden
        
        
        #line 14 "..\..\Login.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Image faceLogin;
        
        #line default
        #line hidden
        
        
        #line 15 "..\..\Login.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Grid content;
        
        #line default
        #line hidden
        
        
        #line 16 "..\..\Login.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox userName;
        
        #line default
        #line hidden
        
        
        #line 17 "..\..\Login.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Label passTip;
        
        #line default
        #line hidden
        
        
        #line 18 "..\..\Login.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.PasswordBox password;
        
        #line default
        #line hidden
        
        
        #line 19 "..\..\Login.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button login;
        
        #line default
        #line hidden
        
        
        #line 20 "..\..\Login.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button cancel;
        
        #line default
        #line hidden
        
        
        #line 21 "..\..\Login.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Image switchBtn;
        
        #line default
        #line hidden
        
        
        #line 22 "..\..\Login.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Label loginTip;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/ControlClient;component/login.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\Login.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            this.topTitle = ((System.Windows.Controls.Grid)(target));
            
            #line 11 "..\..\Login.xaml"
            this.topTitle.MouseLeftButtonDown += new System.Windows.Input.MouseButtonEventHandler(this.TopBar_MouseLeftButtonDown);
            
            #line default
            #line hidden
            return;
            case 2:
            this.faceLogin = ((System.Windows.Controls.Image)(target));
            return;
            case 3:
            this.content = ((System.Windows.Controls.Grid)(target));
            return;
            case 4:
            this.userName = ((System.Windows.Controls.TextBox)(target));
            
            #line 16 "..\..\Login.xaml"
            this.userName.MouseEnter += new System.Windows.Input.MouseEventHandler(this.UserName_MouseEnter);
            
            #line default
            #line hidden
            
            #line 16 "..\..\Login.xaml"
            this.userName.MouseLeave += new System.Windows.Input.MouseEventHandler(this.userName_MouseLeave);
            
            #line default
            #line hidden
            return;
            case 5:
            this.passTip = ((System.Windows.Controls.Label)(target));
            return;
            case 6:
            this.password = ((System.Windows.Controls.PasswordBox)(target));
            
            #line 18 "..\..\Login.xaml"
            this.password.MouseEnter += new System.Windows.Input.MouseEventHandler(this.passWord_MouseEnter);
            
            #line default
            #line hidden
            
            #line 18 "..\..\Login.xaml"
            this.password.MouseLeave += new System.Windows.Input.MouseEventHandler(this.passWord_MouseLeave);
            
            #line default
            #line hidden
            return;
            case 7:
            this.login = ((System.Windows.Controls.Button)(target));
            
            #line 19 "..\..\Login.xaml"
            this.login.Click += new System.Windows.RoutedEventHandler(this.Login_Click);
            
            #line default
            #line hidden
            return;
            case 8:
            this.cancel = ((System.Windows.Controls.Button)(target));
            
            #line 20 "..\..\Login.xaml"
            this.cancel.Click += new System.Windows.RoutedEventHandler(this.Cancel);
            
            #line default
            #line hidden
            return;
            case 9:
            this.switchBtn = ((System.Windows.Controls.Image)(target));
            
            #line 21 "..\..\Login.xaml"
            this.switchBtn.MouseEnter += new System.Windows.Input.MouseEventHandler(this.Image_MouseEnter);
            
            #line default
            #line hidden
            
            #line 21 "..\..\Login.xaml"
            this.switchBtn.MouseLeave += new System.Windows.Input.MouseEventHandler(this.Image_MouseLeave);
            
            #line default
            #line hidden
            
            #line 21 "..\..\Login.xaml"
            this.switchBtn.MouseLeftButtonUp += new System.Windows.Input.MouseButtonEventHandler(this.Image_MouseLeftButtonUp);
            
            #line default
            #line hidden
            return;
            case 10:
            this.loginTip = ((System.Windows.Controls.Label)(target));
            return;
            }
            this._contentLoaded = true;
        }
    }
}
