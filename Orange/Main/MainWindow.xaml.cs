﻿using MahApps.Metro.Controls;
using Orange.MsgBroker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.Net.Json;
using System.IO;
using System.Web;
using System.Security.Permissions;
using System.Reflection;
using Orange.DataManager;
using System.Threading;
using System.Windows.Threading;
using Orange.Util;
using System.Security.Cryptography;
using Microsoft.Win32;
using System.Windows.Controls.Primitives;
using SmartUpdate;

namespace Orange
{
    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]

    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private bool dragStarted = false;
        private bool IsLeftPanelState = false;
        private MusicCollection musicCollection;
        private MusicCollection myPlayListCollection;
        private List<MusicItem> playlist = new List<MusicItem>();
        private MusicItem CurrentItem;
        private CheckBeforeClosing check;
        private Storyboard HideLeftPanelStoryboard;
        private Storyboard ShowLeftPanelStoryboard;
        private Storyboard HideTopGridStoryboard;
        private Storyboard ShowTopGridStoryboard;
        private string queryString;
        private MyfavoritelistMgr myFavoriteMgr;

        private DispatcherTimer Showvideodt;
        private DispatcherTimer dt;
        private double totalTime;
        private double currentTime;

        private int cur_page;

        public MainWindow()
        {
            InitializeComponent();
            

            initStoryboard();
            initUserConfig();

            myFavoriteMgr = MyfavoritelistMgr.instance();
            musicCollection = new MusicCollection();
            myPlayListCollection = new MusicCollection();
            check = new CheckBeforeClosing(new AppInfo(this));
            LoadConfig();
            initLanguage();
            main_menu.SetMusicCollection(musicCollection);
           
            result_musiclist.DataContext = musicCollection;
            myPlayList.DataContext = myPlayListCollection;
            main_page.InitFavoriteListview(myPlayListCollection);

            webBrowser.Navigated += webBrowser_Navigated;
            
            WebBrowserHelper.ClearCache();
                
            String url = Config.YOUTUBEPLAYER_URL;
            webBrowser.Navigate(url);

            (Application.Current as App).msgBroker.MessageReceived += msgBroker_MessageReceived;

            //String volume = webBrowser.InvokeScript("getVolume").ToString();
            dt = new DispatcherTimer();
            dt.Interval = new TimeSpan(0, 0, 0, 0, 500);
            dt.Tick += dt_Tick;

            myPlayListCollection.CollectionChanged += myPlayListCollection_CollectionChanged;

            OpenTutorial();

        }

        private void initFavoritelist()
        {

        }

        private void OpenTutorial()
        {
            if(Properties.Settings.Default.IsFirst)
            {
                tutorialgrid.Children.Add(new tutorial());
                
                
            }            
        }

        private void initLanguage()
        {
            Previousbtn.ToolTip = LanguagePack.Previous();
            Nextbtn.ToolTip = LanguagePack.Next();
            PlayBtn.ToolTip = LanguagePack.l_Play();
            pauseBtn.ToolTip = LanguagePack.l_Pause();
            savebtn.ToolTip = LanguagePack.Save();
            openbtn.ToolTip = LanguagePack.Open();
            deletebtn.ToolTip = LanguagePack.Delete_item();
            bookmarkbtn.ToolTip = LanguagePack.SetBookMark();
            morebtn.Content = LanguagePack.MoreItems();

            selectallitemsbtn.Content = LanguagePack.SelectAllItems();
            addselecteditembtn.Content = LanguagePack.AddSelectedItems();

            if (Config.IsShffle)
            {
                ShuffleBtn.ToolTip = LanguagePack.Shuffle();
            }
            else
            {
                ShuffleBtn.ToolTip = LanguagePack.PlayStraight();
            }


            // DEFAULT 0, ALL REPEAT 1, SINGLE REPEAT 2
            switch (Config.REPEAT)
            {
                case 0:
                    repeatBtn.ToolTip = LanguagePack.NormalMode();
                    break;
                case 1:
                    repeatBtn.ToolTip = LanguagePack.RepeatAllSongs();
                    break;
                case 2:
                    repeatBtn.ToolTip = LanguagePack.RepeatOneSong();
                    break;
            }

            main_menu.SetMenuLanguage();
            

        }


        private void initUserConfig()
        {
            if(UI_Flag.IsShowingVideo){
                ShowVideoBtn.Content = "HideVideo";                
            }
            else { ShowVideoBtn.Content = "ShowVideo"; }
        }

        void myPlayListCollection_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            playlist.Clear();
            CopyPlayList();
        }

        void dt_Tick(object sender, EventArgs e)
        {
            if (!dragStarted)
            {
                totalTime = Double.Parse(webBrowser.InvokeScript("getDuration").ToString());
                currentTime = Double.Parse(webBrowser.InvokeScript("getCurrentTime").ToString());
                
                if(totalTime!=0.0)
                {
                    PlayerSlider.Maximum = totalTime;
                    PlayerSlider.Value = currentTime;

                    TimeSpan ctime = TimeSpan.FromSeconds(currentTime);
                    TimeSpan endtime = TimeSpan.FromSeconds(totalTime);

                    currentTimeTxb.Text = ctime.ToString(@"mm\:ss"); 
                    endTimeTxb.Text = endtime.ToString(@"mm\:ss");

                    if(totalTime == currentTime)
                    {
                        EndMusic();
                    }
                }
                
            }
           
        }

        private void EndMusic()
        {
           // MessageBox.Show("끗");
            dt.Stop();

            if(Config.REPEAT==1)
            {
                for(int i=0 ; i<playlist.Count; i++)
                {
                    if(playlist[i].Equals(CurrentItem))
                    {
                        
                        if(i == playlist.Count-1)
                        {
                            CurrentItem = playlist[0];
                        }
                        else
                        {
                            CurrentItem = playlist[i + 1];
                        }

                        PlayMusic(CurrentItem);

                        return;
                    }
                }
            }
            else if (Config.REPEAT == 0)
            {
                for (int i = 0; i < playlist.Count; i++)
                {
                    if (playlist[i].Equals(CurrentItem))
                    {

                        if (i != playlist.Count - 1)
                        {
                            CurrentItem = playlist[i + 1];
                        }
                        else
                        {
                            Player_State.IsPlaying = false;
                            return;
                        }

                        PlayMusic(CurrentItem);
                        return;
                    }
                }
            }
            else if(Config.REPEAT == 2)
            {
                PlayMusic(CurrentItem);
                return;
            }
        }

        private void SelectCurrentMusicItemInPlayList(MusicItem item)
        {            
            int idx = myPlayListCollection.IndexOf(item);
            myPlayList.SelectedIndex = idx;
        }

        private void CopyPlayList()
        {
            try
            {
                playlist = myPlayListCollection.ToList();

                if (Config.IsShffle)
                {
                    playlist = Shuffle.Randomize(playlist);
                    playlist.Remove(CurrentItem);
                    playlist.Insert(0, CurrentItem);
                }
            }catch(Exception e)
            {
                MessageBox_Orange("Warning", e.Message.ToString());}
            
        }

      

        void msgBroker_MessageReceived(object sender, MsgBroker.MsgBrokerEventArgs e)
        {
            switch(e.Message.MsgOPCode)
            {
                case MESSAGE_MAP.UPLOAD_PLAYLIST:

                    uploadplaylist((string)e.Message.MsgBody);

                    break;
                case MESSAGE_MAP.CREATE_FAVORITE_PLAYLIST:

                    Create_favorite_playlist((string)e.Message.MsgBody);

                    break;

                case MESSAGE_MAP.LOAD_ITEMS_IN_FAVORITE_PLAYLIST:

                    Load_favorite_playlist((string)e.Message.MsgBody);

                    break;
                case UI_CONTROL.PROGRESS_SHOW:
                      if (IsLeftPanelState)
                      {
                          HideLeftPanelStoryboard.Begin();
                          IsLeftPanelState = false;
                      }
                      main_page.Visibility = Visibility.Visible;
                      main_page.SetProgressRing(true, 0);
                    break;

                case UI_CONTROL.PROGRESS_HIDE:
                     main_page.SetProgressRing(false, 0);
                     main_page.Visibility = Visibility.Collapsed;

                     Search_ScrollViewer.ScrollToHome();
                        
                    if(UI_Flag.IsChart)
                    {
                        morebtn.Visibility = Visibility.Collapsed;
                    }
                    

                    break;

                case UI_CONTROL.SHOW_TOP_GRID:
                    if (!UI_Flag.IsShowingTopGrid)
                    {
                        
                        webBrowser.Visibility = Visibility.Hidden;
                                      
                        top_content.Children.Clear();
                        top_content.Children.Add((UserControl)e.Message.MsgBody);
                        ShowTopGridStoryboard.Begin();

                        UI_Flag.IsShowingTopGrid = true;
                    }                   
                    
                    break;
                case UI_CONTROL.HIDE_TOP_GRID:
                    if (UI_Flag.IsShowingTopGrid)
                    {
                        HideTopGridStoryboard.Begin();
                        UI_Flag.IsShowingTopGrid = false;

                        if(UI_Flag.IsShowingVideo && Player_State.IsPlaying && !main_menu.IsFavoritePanel)
                        {
                            webBrowser.Visibility = Visibility.Visible;   
                        }
                    }
                    break;

                case UI_CONTROL.SetTopmost:
                    this.Topmost = true;
                    this.Activate();
                    break;

                case UI_CONTROL.DisableTopmost:
                    this.Topmost = false;
                    this.Activate();
                    break;
                case UI_CONTROL.RefreshMyplayList:
                    myPlayList.DataContext= null;
                    myPlayList.DataContext = myPlayListCollection;
                    break;
                case UI_CONTROL.SET_INIT_AFTER_TUTORIAL:
                    initLanguage();
                    Showvideodt.Start();
                    tutorialgrid.Children.Clear();
                    tutorialgrid.Visibility = Visibility.Collapsed;
                    Properties.Settings.Default.IsFirst = false;
                    break;
                case UI_CONTROL.SET_LANGUAGE:
                    initLanguage();
                    break;
                case UI_CONTROL.ACTIVEMOREBTN:
                    morebtn.Visibility = Visibility.Visible;
                    break;
                case UI_CONTROL.SHOW_VIDEO:
                    if (UI_Flag.IsShowingVideo && Player_State.IsPlaying)
                    {
                        webBrowser.Visibility = Visibility.Visible;
                    }
                    
                    break;
                case UI_CONTROL.HIDE_VIDEO:
                    webBrowser.Visibility = Visibility.Hidden;
                    break;
            }
        }

        private void initStoryboard()
        {
            HideLeftPanelStoryboard = this.Resources["left_panel_hide"] as Storyboard;
            ShowLeftPanelStoryboard = this.Resources["left_panel_show"] as Storyboard;
            HideTopGridStoryboard = this.Resources["Hide_top_content"] as Storyboard;
            HideTopGridStoryboard.Completed += HideTopGridStoryboard_Completed;
            ShowTopGridStoryboard = this.Resources["show_top_content"] as Storyboard;
        }

        void HideTopGridStoryboard_Completed(object sender, EventArgs e)
        {
            top_content.Children.Clear();
        }

        private void MenuBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DrawerMenu();
        }

        private void DrawerMenu()
        {
            // TODO: 여기에 구현된 이벤트 처리기를 추가하십시오.
            if (IsLeftPanelState)
            {
                main_menu.HideFavoriteList();
                HideLeftPanelStoryboard.Begin();
                IsLeftPanelState = false;
                
            }
            else
            {

                ShowLeftPanelStoryboard.Begin();
                IsLeftPanelState = true;
            }
        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            webBrowser.Visibility= Visibility.Visible;
        }

        private void Search_Button_Click(object sender, RoutedEventArgs e)
        {
            SearchOperation();
            morebtn.Visibility = Visibility.Collapsed;
           // MessageBox.Show(resultURL + " " + resultTitle);
        }

        private void SearchOperation(){

            //MessageBox.Show(resultURL + " " + resultTitle);
            if (IsLeftPanelState)
            {

                HideLeftPanelStoryboard.Begin();
                IsLeftPanelState = false;
            }

            Orange.Util.UI_Flag.IsChart = false;
            main_page.Visibility = Visibility.Visible;
            main_page.SetProgressRing(true, 0);
            Search_ScrollViewer.ScrollToHome();
            queryString = searchBox.Text.ToString();
            cur_page = 1;
            
            musicCollection.Clear();

            Thread thread = new Thread(new ThreadStart(SearchingThread));
            thread.Start();

        }


        private void SearchingThread()
        {
            try
            {

                string url = "http://115.71.236.224:8081/searchMusicVideoInformationForPage?query=";
                string query = url + queryString +"&page=" + cur_page;
                JsonArrayCollection items = JSONHelper.getJSONArray(query);
                
                if (items.Count > 0)
                {
                    //MessageBox.Show(col.Count.ToString());

                    Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(delegate
                    {
                        
                        foreach(JsonObjectCollection item in items)
                        {
                            
                            string resultURL = item["url"].GetValue().ToString().Replace("http://www.youtube.com/watch?v=", "");
                            string resultPlayTime = item["time"].GetValue().ToString();
                            string resultTitle = item["title"].GetValue().ToString();
                            MusicItem mitem = new MusicItem();
                            mitem.title = resultTitle;
                            mitem.time = resultPlayTime;
                            mitem.url = resultURL;
                            musicCollection.Add(mitem);

                           
                        }
                        if (items.Count >= 17)
                        {
                            morebtn.Visibility = Visibility.Visible;
                        }
                        main_page.SetProgressRing(false, 0);
                        main_page.Visibility = Visibility.Collapsed;

                    }));

                }
                else
                {
                    Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(delegate
                    {
                        MessageBox_Orange("Warning", "There is no result");
                        
                        main_page.SetProgressRing(false, 0);
                        main_page.Visibility = Visibility.Visible;

                    }));

                }
            }catch(Exception e)
            { MessageBox.Show(e.Message);
                  Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(delegate
                    {
            main_page.SetProgressRing(false, 0);
            main_page.Visibility = Visibility.Visible;
                    }));

            }          
       
        }

        private void morebtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            main_page.Visibility = Visibility.Visible;
            main_page.SetProgressRing(true, 1);
            cur_page++;


           

            Thread thread = new Thread(new ThreadStart(SearchingThread));
            thread.Start();

        }


        private void webBrowser_Loaded(object sender, RoutedEventArgs e)
        {
             Showvideodt = new DispatcherTimer();
             Showvideodt.Interval = new TimeSpan(0, 0, 1);
             Showvideodt.Tick += Showvideodt_Tick;
             if(!Properties.Settings.Default.IsFirst)
             {
                Showvideodt.Start();
             }
             
        }

        void Showvideodt_Tick(object sender, EventArgs e)
        {
            webBrowser.Visibility = Visibility.Visible;
            Showvideodt.Stop();
            Showvideodt.IsEnabled = false;
        }

        public void HideScriptErrors(WebBrowser wb, bool Hide)
        {
            FieldInfo fiComWebBrowser = typeof(WebBrowser).GetField("_axIWebBrowser2", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fiComWebBrowser == null) return;
            object objComWebBrowser = fiComWebBrowser.GetValue(wb);
            if (objComWebBrowser == null) return;
            objComWebBrowser.GetType().InvokeMember("Silent", BindingFlags.SetProperty, null, objComWebBrowser, new object[] { Hide });
        }

        private void webBrowser_Navigated(object sender, NavigationEventArgs e)
        {
            HideScriptErrors(webBrowser, true);

            
            check.CheckUpdate();
         
        }
             

        private void searchBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
        	if(e.Key == Key.Enter)
            {
                SearchOperation();
            }
        }

        #region The result of searching

        private void Load_Music_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            result_musiclist.SelectedItem = (e.OriginalSource as FrameworkElement).DataContext;
            if (result_musiclist.SelectedIndex != -1)
            {

                MusicItem item = (MusicItem)result_musiclist.SelectedItem;
                PlayMusic(item);
                //MessageBox.Show(item.title);

            }
        }

        private void ADD_PlayList_Click(object sender, System.Windows.RoutedEventArgs e)
        {            
            result_musiclist.SelectedItem = (e.OriginalSource as FrameworkElement).DataContext;

            if (result_musiclist.SelectedIndex != -1)
            {
                MusicItem item = (MusicItem)result_musiclist.SelectedItem;
                myPlayListCollection.Add(item);

            }
        }


        private void result_selected_add(object sender, RoutedEventArgs e)
        {
            if (result_musiclist.SelectedIndex != -1)
            {
               foreach (MusicItem item in result_musiclist.SelectedItems)
               {
                   myPlayListCollection.Add(item);

                   
               }
               result_musiclist.SelectedIndex = -1;
            }
        }

        private void result_all_select(object sender, System.Windows.RoutedEventArgs e)
        {
            result_musiclist.SelectAll();
        }

        private void result_musiclist_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = ((FrameworkElement)e.OriginalSource).DataContext as MusicItem;
            if (item != null)
            {
                //MessageBox.Show(item.title);
               // myPlayListCollection.Add(item);
                PlayMusic(item);

                //MessageBox.Show(item.title + " " + item.url);
            }
        }
        #endregion


        #region play list event
        private void Show_Video_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            myPlayList.SelectedItem = (e.OriginalSource as FrameworkElement).DataContext;

            if (myPlayList.SelectedIndex != -1)
            {
                MusicItem item = (MusicItem)myPlayList.SelectedItem;

   
                    webBrowser.Visibility = Visibility.Visible;
                    PlayMusic(item);
   
                
                //string delimiter = "http://www.youtube.com/watch?v=";
               // string s = item.url.Replace("http://www.youtube.com/watch?v=","");
                //MessageBox.Show(target);


            }
        }

        private void myPlayList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = ((FrameworkElement)e.OriginalSource).DataContext as MusicItem;
            if (item != null)
            {
               // MessageBox.Show("Item's Double Click handled!");
                PlayMusic(item);

                CopyPlayList();
                
            }

        }


        private void Lyric_PlayList_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            myPlayList.SelectedItem = (e.OriginalSource as FrameworkElement).DataContext;

            if (myPlayList.SelectedIndex != -1)
            {
                MusicItem item = (MusicItem)myPlayList.SelectedItem;

                //MessageBox.Show(item.title);
                //myPlayListCollection.Add(item);
             
            }

        }

        private void URL_Copy_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            myPlayList.SelectedItem = (e.OriginalSource as FrameworkElement).DataContext;

            if (myPlayList.SelectedIndex != -1)
            {
                MusicItem item = (MusicItem)myPlayList.SelectedItem;

                string strURL = "http://www.youtube.com/watch?v=" + item.url;
                Clipboard.SetText(strURL);

                MessageBox.Show(strURL);
                //myPlayListCollection.Add(item);

            }

        }

        private void MP3_Convert_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            myPlayList.SelectedItem = (e.OriginalSource as FrameworkElement).DataContext;

            if (myPlayList.SelectedIndex != -1)
            {
                  var dialog = new System.Windows.Forms.FolderBrowserDialog();
                  System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if(result == System.Windows.Forms.DialogResult.OK)
                {

                    MusicItem item = (MusicItem)myPlayList.SelectedItem;
                    ConvertMP3.PATH = dialog.SelectedPath;
                    //MessageBox.Show(path);

                    ConvertMP3.URL = "http://www.youtube.com/watch?v=" + item.url;

                    MsgBroker.MsgBrokerMsg arg = new MsgBroker.MsgBrokerMsg();
                    arg.MsgOPCode = UI_CONTROL.SHOW_TOP_GRID;
                    arg.MsgBody = new ConvertingProgress();
                    (Application.Current as App).msgBroker.SendMessage(arg);


                    ConvertMP3.worker(this, ConvertMP3.URL, ConvertMP3.PATH, WKIND.CONVERT);
                 
                  
                }
                  
                // string mUrl = "http://www.youtube.com/watch?v="+ item.url;

               // ConvertMP3.worker(mUrl, )

                //MessageBox.Show(item.title);
                //myPlayListCollection.Add(item);

            }

        }




        #endregion


        #region player controller

        private void play(object sender, RoutedEventArgs e)
        {
            //var document = webBrowser.Document;
            //webBrowser.Document.GetType().InvokeMember("pauseVideo", BindingFlags.InvokeMethod, null, document, null);
            if(Player_State.IsPlaying)
            {
                webBrowser.InvokeScript("playVideo");
                dt.Start();
            }
            else
            {
                if (myPlayList.SelectedIndex != -1)
                {

                    MusicItem item = (MusicItem)myPlayList.SelectedItem;
                    PlayMusic(item);
                    //MessageBox.Show(item.title);
                    webBrowser.InvokeScript("playVideo");
                    dt.Start();
                    Player_State.IsPlaying = true;
                }                
            }
            
           
            //PlayBtn.Template = (ControlTemplate)FindResource("PauseButtonControlTemplate");
        }

        private void pause(object sender, System.Windows.RoutedEventArgs e)
        {
            webBrowser.InvokeScript("pauseVideo");
            dt.Stop();
        }

        private void Next_Music(object sender, RoutedEventArgs e)
        {
            EndMusic();
        }

        private void Previous_music(object sender, RoutedEventArgs e)
        {
            dt.Stop();

            if(Config.REPEAT==2)
            {
                PlayMusic(CurrentItem);
                return;
            }

            for (int i = 0; i < playlist.Count; i++)
            {
                 
                
                if (playlist[i].Equals(CurrentItem))
                 {

                        if (i != 0)
                        {
                           CurrentItem = playlist[i - 1];
                        }else if(i==0)
                        {
                            CurrentItem = playlist[playlist.Count-1];
                        }

                        PlayMusic(CurrentItem);
                        return;
                    }
            }
            
        }

        private void set_shuffle(object sender, RoutedEventArgs e)
        {
            if(Config.IsShffle)
            {
                Config.IsShffle = false;
                ShuffleBtn.Template = (ControlTemplate)FindResource("NonShuffleButtonControlTemplate");
                ShuffleBtn.ToolTip = LanguagePack.PlayStraight();
            }
            else
            {
                Config.IsShffle = true;                
                ShuffleBtn.Template = (ControlTemplate)FindResource("ShuffleButtonControlTemplate");
                ShuffleBtn.ToolTip = LanguagePack.Shuffle();
            }
            CopyPlayList();
        }

        private void set_repeat(object sender, RoutedEventArgs e)
        {
            
            // DEFAULT 0, ALL REPEAT 1, SINGLE REPEAT 2
            switch ((++Config.REPEAT)%3)
            {
                case 0:
                    repeatBtn.Template = (ControlTemplate)FindResource("DefaultRepeatButtonControlTemplate");
                    repeatBtn.ToolTip = LanguagePack.NormalMode();
                    Config.REPEAT = 0;
                    break;
                case 1:
                    repeatBtn.Template = (ControlTemplate)FindResource("RepeatButtonControlTemplate");
                    repeatBtn.ToolTip = LanguagePack.RepeatAllSongs();
                    Config.REPEAT = 1;
                    break;
                case 2:
                    repeatBtn.Template = (ControlTemplate)FindResource("SingleRepeatButtonControlTemplate");
                    repeatBtn.ToolTip = LanguagePack.RepeatOneSong();
                    Config.REPEAT = 2;
                    break;
            }
             

        }

        private void Show_video_in_control(object sender, RoutedEventArgs e)
        {
            if(webBrowser.Visibility == Visibility.Visible)
            {
                webBrowser.Visibility = Visibility.Hidden;
                ShowVideoBtn.Content = "ShowVideo";
                UI_Flag.IsShowingVideo = false;

            }else{
                webBrowser.Visibility = Visibility.Visible;
                ShowVideoBtn.Content = "HideVideo";
                UI_Flag.IsShowingVideo = true;
            }            
        }



        private void Mute(object sender, System.Windows.RoutedEventArgs e)
        {
            if (webBrowser.IsLoaded && Player_State.IsPlaying)
            {
                if (Config.IsMute)
                {
                    Config.IsMute = false;
                    VolumeBtn.Template = (ControlTemplate)FindResource("VolButtonControlTemplate");

                    webBrowser.InvokeScript("unMute");
                }
                else
                {
                    Config.IsMute = true;
                    VolumeBtn.Template = (ControlTemplate)FindResource("MuteButtonControlTemplate");
                    webBrowser.InvokeScript("mute");
                }
            }
        }
        #endregion

        #region playlist event controller
        private void top_list(object sender, RoutedEventArgs e)
        {
            if (myPlayList.SelectedIndex != -1)
            {
                MusicItem item = (MusicItem)myPlayList.SelectedItem;


               // MessageBox.Show(item.title);
                myPlayListCollection.Remove(item);
                myPlayListCollection.Insert(0, item);

                myPlayList.SelectedItem = item;
            }
        }

        private void up_list(object sender, RoutedEventArgs e)
        {
            int idx = myPlayList.SelectedIndex;
            if (myPlayList.SelectedIndex != -1)
            {
                if (myPlayList.SelectedIndex == 0)
                    return;

                MusicItem item = (MusicItem)myPlayList.SelectedItem;


                // MessageBox.Show(item.title);
                myPlayListCollection.Remove(item);
                myPlayListCollection.Insert(idx-1, item);

                myPlayList.SelectedItem = item;
            }
        }

        private void down_list(object sender, RoutedEventArgs e)
        {
            int idx = myPlayList.SelectedIndex;
            if (myPlayList.SelectedIndex != -1)
            {
                if (myPlayList.SelectedIndex == myPlayListCollection.Count-1)
                    return;

                MusicItem item = (MusicItem)myPlayList.SelectedItem;


                // MessageBox.Show(item.title);
                myPlayListCollection.Remove(item);
                myPlayListCollection.Insert(idx + 1, item);

                myPlayList.SelectedItem = item;
            }
        }

        private void bottom_list(object sender, RoutedEventArgs e)
        {
            int idx = myPlayList.SelectedIndex;
            if (myPlayList.SelectedIndex != -1)
            {
                if (myPlayList.SelectedIndex == myPlayListCollection.Count-1)
                    return;

                MusicItem item = (MusicItem)myPlayList.SelectedItem;


                // MessageBox.Show(item.title);
                myPlayListCollection.Remove(item);
                myPlayListCollection.Insert(myPlayListCollection.Count, item);

                myPlayList.SelectedItem = item;
            }
        }

        private void save_list(object sender, RoutedEventArgs e)
        {
            
            string fn = DateTime.Now.ToString("yyyy-MM-dd_HH:mm:ss");    //2013-07-16 14:10:26
            SaveFileDialog sfDialog = new SaveFileDialog();
            sfDialog.FileName = fn;
            sfDialog.Title = "Save";
            sfDialog.Filter = "orm files (*.orm)|*.orm";
            sfDialog.OverwritePrompt = true;
            sfDialog.CheckPathExists = true;

            
            if (sfDialog.ShowDialog() == true)
            {

                File.WriteAllText(sfDialog.FileName, Security.Encrypt(Newtonsoft.Json.JsonConvert.SerializeObject(myPlayListCollection)));
            }
        }

        private void open_list(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofDialog = new OpenFileDialog();
            Nullable<bool> result = ofDialog.ShowDialog();
            ofDialog.DefaultExt = "*.orm";
            ofDialog.Filter = "orm files (*.orm)|*.orm";


            if (result == true)
            {
                
                string fileName = ofDialog.FileName;
                try
                {
                    string json = Security.Decrypt(File.ReadAllText(fileName));
                    var playerList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<MusicItem>>(json);
                    myPlayListCollection.Clear();
                    foreach (var it in playerList)
                    {
                        myPlayListCollection.Add(it);
                    }
                }catch(Exception)
                {

                    MessageBox.Show("File type is not correct");
                }           
                                
            }
        }


        private void delete_list(object sender, RoutedEventArgs e)
        {
            if (myPlayList.SelectedIndex != -1)
            {
                List<MusicItem> it = new List<MusicItem>();
                foreach (MusicItem item in myPlayList.SelectedItems)
                {
                    it.Add(item);
                }
                foreach (MusicItem item in it)
                {
                    myPlayListCollection.Remove(item);
                }
                it.Clear();
            }
        }
        #endregion



        #region Slider Controller
        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!dragStarted)
            {
                if (webBrowser.IsLoaded && Player_State.IsPlaying && !Config.IsMute)
                {
                    webBrowser.InvokeScript("setVolume", new String[] { VolumeSlider.Value.ToString() });
                    Player_State.VolumeValue = VolumeSlider.Value.ToString();
                }
                
            }

            
        }
        private void VolumeSlider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {

            if (webBrowser.IsLoaded && Player_State.IsPlaying && !Config.IsMute && IsActive)
            {
                webBrowser.InvokeScript("setVolume", new String[] { VolumeSlider.Value.ToString() });
                Player_State.VolumeValue = VolumeSlider.Value.ToString();
            }
            this.dragStarted = false;

        }

        private void VolumeSlider_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            this.dragStarted = true;
        }


        
        private void PlayerSlider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            
                //MessageBox.Show("DragCompleted");
                //webBrowser.InvokeScript("loadVideoById", new String[] { PlayerSlider.Value.ToString() });
                webBrowser.InvokeScript("seekTo", new String[] { PlayerSlider.Value.ToString() });
                Player_State.IsPlaying = true;
                dt.Start();

            this.dragStarted = false;
        }

        private void PlayerSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!dragStarted)
            {
//                 webBrowser.InvokeScript("seekTo", new String[] { PlayerSlider.Value.ToString() });
//                 Player_State.IsPlaying = true;
//                 dt.Start();
            }
        }

        private void PlayerSlider_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
                this.dragStarted = true;
                dt.Stop();           
        }
        

        private void PlayerSlider_MouseDown(object sender, MouseButtonEventArgs e)
        {

            if(!dragStarted)
            {
                dt.Stop();
               // e.Handled = true;
            }      

        }

        private void PlayerSlider_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!dragStarted)
            {
                webBrowser.InvokeScript("seekTo", new String[] { PlayerSlider.Value.ToString() });
                Player_State.IsPlaying = true;
                dt.Start();
                e.Handled = true;
            }      
        }



        #endregion

        private void PlayMusic(MusicItem item)
        {
            try {
                if (PlayerSlider.IsEnabled == false)
                    PlayerSlider.IsEnabled = true;

                if (UI_Flag.IsShowingVideo && !main_menu.IsFavoritePanel)
                {
                    webBrowser.Visibility = Visibility.Visible;
                }
                else if (!UI_Flag.IsShowingVideo || main_menu.IsFavoritePanel) 
                { webBrowser.Visibility = Visibility.Hidden; }
               
                WebBrowserHelper.ClearCache();
                
                webBrowser.InvokeScript("loadVideoById", new String[] { item.url });
             
                dt.Start();
                Player_State.IsPlaying = true;
                webBrowser.InvokeScript("setVolume", new String[] { Player_State.VolumeValue });

                CurrentItem = item;
                Music_title.Text = item.title;
                SelectCurrentMusicItemInPlayList(item);
            }
            catch (Exception e) {
                MessageBox_Orange("Warning", "Try again.\n\n" + e.Message.ToString());
                
                
            }
            
        }



        private void Information_Click(object sender, RoutedEventArgs e)
        {
            //information_uc.Visibility = Visibility.Visible;
            
            if(!Properties.Settings.Default.IsFirst)
            {
                MsgBroker.MsgBrokerMsg arg = new MsgBroker.MsgBrokerMsg();
                arg.MsgOPCode = UI_CONTROL.SHOW_TOP_GRID;
                arg.MsgBody = new information_usercontrol(this);
                (Application.Current as App).msgBroker.SendMessage(arg);
            }
            
        }

        private void myPlayList_KeyDown(object sender, KeyEventArgs e)
        {
            switch(e.Key)
            {
                case Key.Delete:
                    if (myPlayList.SelectedIndex != -1)
                    {
                        List<MusicItem> it = new List<MusicItem>();
                       // myPlayList.DataContext = null;
                        foreach (MusicItem item in myPlayList.SelectedItems)
                        {
                            it.Add(item);                          
                        }                       
                        foreach (MusicItem item in it)
                        {
                            myPlayListCollection.Remove(item);
                        }
                        it.Clear();
                    }
                    break;
                case Key.Up:
                    if (myPlayList.SelectedIndex > 0 )
                    {                        
                        myPlayList.SelectedIndex = myPlayList.SelectedIndex - 1;                        
                    }
                    break;
                case Key.Down:
                    if (myPlayList.SelectedIndex != -1 || myPlayList.SelectedIndex != (myPlayListCollection.Count-1) )
                    {
                        myPlayList.SelectedIndex = myPlayList.SelectedIndex +1;
                    }
                    break;
            }

        }


        private void Search_ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scv = (ScrollViewer)sender;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
            e.Handled = true;
        }

        private void test_click(object sender, RoutedEventArgs e)
        {
            string msg = "";
            for(int i=0; i<playlist.Count; i++)
            {
                msg += playlist[i].title + " | ";
            }

            MessageBox.Show(msg);
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

            if (check.IsAvailableUpdate)
            {
                UpdateWindow updatewin = new UpdateWindow();
                updatewin.initSmartUpdate(new AppInfo(this));
                bool result = (bool)updatewin.ShowDialog();

                if(result==true)
                {

                }
                else if (result == false)
                {
                    //MessageBox.Show("the update download was cancelled.");
                }
            }

           // SaveTempList();
            SaveConfig();
            musicCollection.Clear();
            myPlayListCollection.Clear();
            WebBrowserHelper.ClearCache();
            webBrowser.Dispose();
            (Application.Current as App).msgBroker.MessageReceived -= msgBroker_MessageReceived;

        }
        private void LoadConfig()
        {
            Config.Language_for_Orange = Properties.Settings.Default.Language_for_Orange;
            LanguagePack.TYPE = Config.Language_for_Orange;
            Config.IsShffle = Properties.Settings.Default.IsShffle;
            if (!Config.IsShffle)
            {
                ShuffleBtn.Template = (ControlTemplate)FindResource("NonShuffleButtonControlTemplate");
                ShuffleBtn.ToolTip = LanguagePack.PlayStraight();

                
            }
            else
            {
                ShuffleBtn.Template = (ControlTemplate)FindResource("ShuffleButtonControlTemplate");
                ShuffleBtn.ToolTip = LanguagePack.Shuffle();
            }

            Config.REPEAT = Properties.Settings.Default.REPEAT;
            switch (Config.REPEAT)
            {
                case 0:
                    repeatBtn.Template = (ControlTemplate)FindResource("DefaultRepeatButtonControlTemplate");
                    repeatBtn.ToolTip = LanguagePack.NormalMode();
                    break;
                case 1:
                    repeatBtn.Template = (ControlTemplate)FindResource("RepeatButtonControlTemplate");
                    repeatBtn.ToolTip = LanguagePack.RepeatAllSongs();
                    break;
                case 2:
                    repeatBtn.Template = (ControlTemplate)FindResource("SingleRepeatButtonControlTemplate");
                    repeatBtn.ToolTip = LanguagePack.RepeatOneSong();
                    break;
            }
            Config.IsMute = Properties.Settings.Default.IsMute;
            Config.Language_for_Orange = Properties.Settings.Default.Language_for_Orange;
            

            if (!Properties.Settings.Default.Playlist.Equals(""))
            {
                try
                {
                    string json = Security.Decrypt(Properties.Settings.Default.Playlist);
                    var playerList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PlaylistItem>>(json);
                    myFavoriteMgr.MyfavoriteCollection.Clear();
                    
                    
                    string filedirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\OrangePlayer\favoritePlaylist";
                    
                    foreach (var it in playerList)
                    {
                        string filepath = string.Format("{0}/{1}.orm", filedirectory, it.name);
                        if(File.Exists(filepath))
                            myFavoriteMgr.MyfavoriteCollection.Add(it);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }    
            }
             
            
        }

        private void SaveConfig()
        {
            //myFavoriteMgr.MyfavoriteCollection.Clear();
            Properties.Settings.Default.IsShffle = Config.IsShffle;
            Properties.Settings.Default.REPEAT =  Config.REPEAT;
            Properties.Settings.Default.IsMute =  Config.IsMute;
            Properties.Settings.Default.Language_for_Orange = Config.Language_for_Orange;
            Properties.Settings.Default.Playlist = Security.Encrypt(Newtonsoft.Json.JsonConvert.SerializeObject(myFavoriteMgr.MyfavoriteCollection));
           // Properties.Settings.Default.Playlist = "";
            Properties.Settings.Default.Save();
        }


        private void Config_Click(object sender, RoutedEventArgs e)
        {
            if(!Properties.Settings.Default.IsFirst)
            {
                MsgBroker.MsgBrokerMsg arg = new MsgBroker.MsgBrokerMsg();
                arg.MsgOPCode = UI_CONTROL.SHOW_TOP_GRID;
                arg.MsgBody = new Preferences();
                (Application.Current as App).msgBroker.SendMessage(arg);
            }

        }



        private void currentTagNotContactsList_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
        {
            ScrollBar sb = e.OriginalSource as ScrollBar;

            if (sb.Orientation == Orientation.Horizontal)
                return;

            if (sb.Value == sb.Maximum && !UI_Flag.IsChart)
            {
                morebtn.Visibility = Visibility.Visible;
            }          
        }

        private void Bn_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start(Config.GIFT_URL);
        }

        private void OnListViewItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.Handled)
                return;

            ListViewItem item = Orange.Util.VisualTreeHelper.FindParent<ListViewItem>((DependencyObject)e.OriginalSource);

            
            if (item == null)
                return;
            if (item.Focusable && !item.IsFocused)
                item.Focus();


            //MessageBox.Show("test");
        }


        private void Remove_PlaylistItem_Click(object sender, RoutedEventArgs e)
        {
            if (myPlayList.SelectedIndex != -1)
            {
                List<MusicItem> it = new List<MusicItem>();
                foreach (MusicItem item in myPlayList.SelectedItems)
                {
                    it.Add(item);
                }
                foreach (MusicItem item in it)
                {
                    myPlayListCollection.Remove(item);
                }
                it.Clear();
            }
        }

        private void Rename_PlaylistItem_Click(object sender, RoutedEventArgs e)
        {
            if (myPlayList.SelectedIndex != -1)
            {                
                MusicItem item = (MusicItem)myPlayList.SelectedItem;
                MsgBroker.MsgBrokerMsg arg = new MsgBroker.MsgBrokerMsg();
                arg.MsgOPCode = UI_CONTROL.SHOW_TOP_GRID;
                arg.MsgBody = new rename_usercontrol(item);
                (Application.Current as App).msgBroker.SendMessage(arg);
            }
        }


        private void Create_favorite_playlist(string filename)
        {
            try
            {
                
                String path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string filedirectory = path + @"\OrangePlayer\favoritePlaylist";
                if (!Directory.Exists(filedirectory))
                    Directory.CreateDirectory(filedirectory);


                string filepath = string.Format("{0}/{1}.orm", filedirectory, filename);
                //System.AppDomain.CurrentDomain.BaseDirectory 
                //System.IO.Directory.GetCurrentDirectory();
                File.WriteAllText(filepath, Security.Encrypt(Newtonsoft.Json.JsonConvert.SerializeObject(myPlayListCollection)));

                if (File.Exists(filepath))
                {
                    PlaylistItem item = new PlaylistItem();
                    item.name = filename;
                    item.filePath = filepath;

                    myFavoriteMgr.MyfavoriteCollection.Add(item);

                }
                else { MessageBox_Orange("Warning", "failed to create the file"); }
            }catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            
        }

        private void Load_favorite_playlist(string filename)
        {
            String path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string filedirectory = path + @"\OrangePlayer\favoritePlaylist";
                        
            string filepath = string.Format("{0}/{1}.orm", filedirectory, filename);


            if (File.Exists(filepath))
            {
                try
                {
                    string json = Security.Decrypt(File.ReadAllText(filepath));
                    var playerList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<MusicItem>>(json);
                    myPlayListCollection.Clear();
                    foreach (var it in playerList)
                    {
                        myPlayListCollection.Add(it);
                    }
                    MessageBox_Orange("SUCCESS", "Playlist was successfully loaded");
                    
                }
                catch (Exception)
                {
                    MessageBox_Orange("Warning", "File type is not correct");
                    
                }

            }
            else
            {
                MessageBox_Orange("Warning", "failed to create the file");
            }
                

        }

        private void createfavoritelist_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (myPlayListCollection.Count == 0)
            {
                MessageBox_Orange("Warning","Playlist is empty.");
                
                return;
            }
                
            MsgBroker.MsgBrokerMsg arg = new MsgBroker.MsgBrokerMsg();
            arg.MsgOPCode = UI_CONTROL.SHOW_TOP_GRID;
            arg.MsgBody = new Create_FavoritePlaylistUserControl();
            (Application.Current as App).msgBroker.SendMessage(arg);
            main_menu.ShowFavoriteList();
            DrawerMenu();
            
        	
        }

        private void uploadplaylist(string title)
        {

            Playlist myChart = new Playlist();
            myChart.chart_name = title;
            myChart.chart_list = myPlayListCollection.ToList();
            if(myChart.chart_list.Count==0)
            {
                MessageBox_Orange("Warning", "Upload ERROR");
                
                return;
            }

            string uri = @"http://115.71.236.224:8081/uploadPlayList";
            string request_data = Security.Encrypt(Newtonsoft.Json.JsonConvert.SerializeObject(myChart));

            WebClient webClient = new WebClient();
            webClient.Headers[HttpRequestHeader.ContentType] = "application/json";
            webClient.Encoding = UTF8Encoding.UTF8;

            try
            {
                string responseJson = webClient.UploadString(uri, request_data);
                if (responseJson.Equals("True"))
                {
                    MessageBox_Orange("Notice", "Your list has successfully uploaded");
                    main_menu.RecentPlaylist();
                }

                    
            }
            catch (Exception ex)
            {
                MessageBox_Orange("Warning", ex.Message.ToString());
            }
        }

        private void MessageBox_Orange(string title, string content)
        {
            MsgBroker.MsgBrokerMsg arg = new MsgBroker.MsgBrokerMsg();
            arg.MsgOPCode = UI_CONTROL.SHOW_TOP_GRID;
            arg.MsgBody = new DialogMsg(title, content);
            (Application.Current as App).msgBroker.SendMessage(arg);
        }
    }

}
