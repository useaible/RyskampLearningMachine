using Challenger.Models;
using ChallengerLib.Models;
using RLV.Core.Interfaces;
using RLV.Core.Models;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ToastNotifications;
using ToastNotifications.Lifetime;
using ToastNotifications.Position;

namespace Challenger
{
    /// <summary>
    /// Interaction logic for RLVPanel.xaml
    /// </summary>
    public partial class VisualizerWindow : Window
    {
        private IRLVCore core;
        private IRLVScaleSelectionPanel detailsScalePanel;
        private IRLVScaleSelectionPanel chartScalePanel;
        private IRLVOutputVisualizer visualizer;
        private int maxMoves = 0;
        private IEnumerable<MoveDetails> predictedMoves;
        private List<MoveDetails> allMoves;

        private Notifier notifier;
        public Notifier Notification { get { return notifier; } }

        public VisualizerWindow(IRLVCore core)
        {
            InitializeComponent();

            this.core = core;
            this.visualizer = new RLVOutputVisualizer(this);

            IRLVScaleSelectionVM scaleVM = new RLVScaleSelectionVM();

            detailsScalePanel = rlv.DetailsControl.ScalePanel;
            chartScalePanel = rlv.ChartControl.ScalePanel;

            detailsScalePanel.SetViewModel(scaleVM);
            chartScalePanel.SetViewModel(scaleVM);
            scaleVM.DefaultScale = 100;

            core.SetupVisualizer(new List<IRLVPanel>
            {
                rlv.DetailsControl,
                rlv.ChartControl,
                chartScalePanel,
                detailsScalePanel
            }, visualizer);

            this.Closing += VisualizerWindow_Closing;

            notifier = new Notifier(cfg =>
            {
                cfg.PositionProvider = new WindowPositionProvider(
                    parentWindow: this,
                    corner: Corner.TopRight,
                    offsetX: 10,
                    offsetY: 10);

                cfg.LifetimeSupervisor = new TimeAndCountBasedLifetimeSupervisor(
                    notificationLifetime: TimeSpan.FromSeconds(3),
                    maximumNotificationCount: MaximumNotificationCount.FromCount(5));

                cfg.Dispatcher = Application.Current.Dispatcher;
            });
        }

        public VisualizerWindow(IRLVCore core, int maxMoves, IEnumerable<MoveDetails> predictedMoves)
            : this(core)
        {
            this.maxMoves = maxMoves;
            this.predictedMoves = predictedMoves;

            this.radioAllMoves.IsChecked = true;
            this.radioRecentMoves.IsChecked = false;

            this.allMoves = new List<MoveDetails>();

            ToggleComparisonMode(false);
        }

        private void SetAllMoves()
        {
            if (lvMoves.Children.Count == 0)
            {
                this.allMoves = new List<MoveDetails>();
                for (int a = 0; a < this.maxMoves; a++)
                {
                    allMoves.Add(new MoveDetails { MoveNumber = a + 1 });
                }

                foreach (MoveDetails mv in allMoves)
                {
                    StackPanel pnl = new StackPanel();
                    pnl.Orientation = Orientation.Horizontal;

                    TextBlock txt = new TextBlock();
                    txt.HorizontalAlignment = HorizontalAlignment.Left;
                    txt.TextWrapping = TextWrapping.Wrap;
                    txt.Text = mv.MoveNumber.ToString();
                    txt.VerticalAlignment = VerticalAlignment.Top;

                    var upMove = new MoveDetails() { MoveNumber = mv.MoveNumber, Direction = 0 };
                    Rectangle up = new Rectangle();
                    up.Width = 20;
                    up.Height = 20;
                    up.Margin = new Thickness(20, 0, 0, 5);
                    up.Fill = new ImageBrush { ImageSource = new BitmapImage(new Uri(System.IO.Path.Combine("Images", "arrow-up.png"), UriKind.Relative)) };
                    up.HorizontalAlignment = HorizontalAlignment.Stretch;
                    up.VerticalAlignment = VerticalAlignment.Top;
                    up.Stretch = Stretch.Uniform;
                    up.Cursor = Cursors.Hand;
                    up.MouseEnter += Dir_MouseEnter;
                    up.MouseLeave += Dir_MouseLeave;
                    up.InputBindings.Add(new MouseBinding()
                    {
                        Gesture = new MouseGesture(MouseAction.LeftClick),
                        Command = new ItemClickedCommand(() =>
                        {
                            OnArrowClick(upMove);
                        })
                    });

                    var rightMove = new MoveDetails() { MoveNumber = mv.MoveNumber, Direction = 1 };
                    Rectangle right = new Rectangle();
                    right.Width = 20;
                    right.Height = 20;
                    right.Margin = new Thickness(20, 0, 0, 5);
                    right.Fill = new ImageBrush { ImageSource = new BitmapImage(new Uri(System.IO.Path.Combine("Images", "arrow-right.png"), UriKind.Relative)) };
                    right.HorizontalAlignment = HorizontalAlignment.Stretch;
                    right.VerticalAlignment = VerticalAlignment.Top;
                    right.Stretch = Stretch.Uniform;
                    right.Cursor = Cursors.Hand;
                    right.MouseEnter += Dir_MouseEnter;
                    right.MouseLeave += Dir_MouseLeave;
                    right.InputBindings.Add(new MouseBinding()
                    {
                        Gesture = new MouseGesture(MouseAction.LeftClick),
                        Command = new ItemClickedCommand(() =>
                        {
                            OnArrowClick(rightMove);
                        })
                    });

                    var downMove = new MoveDetails() { MoveNumber = mv.MoveNumber, Direction = 2 };
                    Rectangle down = new Rectangle();
                    down.Width = 20;
                    down.Height = 20;
                    down.Margin = new Thickness(20, 0, 0, 5);
                    down.Fill = new ImageBrush { ImageSource = new BitmapImage(new Uri(System.IO.Path.Combine("Images", "arrow-down.png"), UriKind.Relative)) };
                    down.HorizontalAlignment = HorizontalAlignment.Stretch;
                    down.VerticalAlignment = VerticalAlignment.Top;
                    down.Stretch = Stretch.Uniform;
                    down.Cursor = Cursors.Hand;
                    down.MouseEnter += Dir_MouseEnter;
                    down.MouseLeave += Dir_MouseLeave;
                    down.InputBindings.Add(new MouseBinding()
                    {
                        Gesture = new MouseGesture(MouseAction.LeftClick),
                        Command = new ItemClickedCommand(() =>
                        {
                            OnArrowClick(downMove);
                        })
                    });

                    var leftMove = new MoveDetails() { MoveNumber = mv.MoveNumber, Direction = 3 };
                    Rectangle left = new Rectangle();
                    left.Width = 20;
                    left.Height = 20;
                    left.Margin = new Thickness(20, 0, 0, 5);
                    left.Fill = new ImageBrush { ImageSource = new BitmapImage(new Uri(System.IO.Path.Combine("Images", "arrow-left.png"), UriKind.Relative)) };
                    left.HorizontalAlignment = HorizontalAlignment.Stretch;
                    left.VerticalAlignment = VerticalAlignment.Top;
                    left.Stretch = Stretch.Uniform;
                    left.Cursor = Cursors.Hand;
                    left.MouseEnter += Dir_MouseEnter;
                    left.MouseLeave += Dir_MouseLeave;
                    left.InputBindings.Add(new MouseBinding()
                    {
                        Gesture = new MouseGesture(MouseAction.LeftClick),
                        Command = new ItemClickedCommand(() =>
                        {
                            OnArrowClick(leftMove);
                        })
                    });

                    pnl.Children.Add(txt);
                    pnl.Children.Add(up);
                    pnl.Children.Add(right);
                    pnl.Children.Add(down);
                    pnl.Children.Add(left);

                    lvMoves.Children.Add(pnl);
                }
            }
        }

        private void SetPredictedMoves()
        {
            if (lvPredictedMoves.Children.Count == 0)
            {
                foreach (MoveDetails mv in predictedMoves)
                {
                    StackPanel pnl = new StackPanel();
                    pnl.Orientation = Orientation.Horizontal;

                    TextBlock txt = new TextBlock();
                    txt.HorizontalAlignment = HorizontalAlignment.Left;
                    txt.TextWrapping = TextWrapping.Wrap;
                    txt.Text = mv.MoveNumber.ToString();
                    txt.VerticalAlignment = VerticalAlignment.Top;

                    Rectangle dir = new Rectangle();
                    dir.Width = 20;
                    dir.Height = 20;
                    dir.Cursor = Cursors.Hand;
                    dir.MouseEnter += Dir_MouseEnter;
                    dir.MouseLeave += Dir_MouseLeave;
                    dir.Margin = new Thickness(20, 0, 0, 5);

                    if (mv.Direction == 0) //up
                    {
                        dir.Fill = new ImageBrush { ImageSource = new BitmapImage(new Uri(System.IO.Path.Combine("Images", "arrow-up.png"), UriKind.Relative)) };
                    }
                    else if (mv.Direction == 1) //right
                    {
                        dir.Fill = new ImageBrush { ImageSource = new BitmapImage(new Uri(System.IO.Path.Combine("Images", "arrow-right.png"), UriKind.Relative)) };
                    }
                    else if (mv.Direction == 2) //down
                    {
                        dir.Fill = new ImageBrush { ImageSource = new BitmapImage(new Uri(System.IO.Path.Combine("Images", "arrow-down.png"), UriKind.Relative)) };
                    }
                    else if (mv.Direction == 3) //left
                    {
                        dir.Fill = new ImageBrush { ImageSource = new BitmapImage(new Uri(System.IO.Path.Combine("Images", "arrow-left.png"), UriKind.Relative)) };
                    }

                    dir.HorizontalAlignment = HorizontalAlignment.Stretch;
                    dir.VerticalAlignment = VerticalAlignment.Top;
                    dir.Stretch = Stretch.Uniform;
                    dir.InputBindings.Add(new MouseBinding()
                    {
                        Gesture = new MouseGesture(MouseAction.LeftClick),
                        Command = new ItemClickedCommand(() =>
                        {
                            OnArrowClick(mv);
                        })
                    });

                    pnl.Children.Add(txt);
                    pnl.Children.Add(dir);

                    lvPredictedMoves.Children.Add(pnl);
                }
            }
        }

        private void OnArrowClick(MoveDetails move)
        {
            var inputs = new List<IRLVIOValues>()
            {
                new RLVIOValues() { IOName = "Move", Value = move.MoveNumber.ToString() }
            };

            var outputs = new List<IRLVIOValues>()
            {
                new RLVIOValues() { IOName = "Direction", Value = move.Direction.ToString() }
            };

            visualizer.SelectNewItem(inputs, outputs, null);
        }

        private void Dir_MouseLeave(object sender, MouseEventArgs e)
        {
            Rectangle rect = (Rectangle)sender;
            rect.Stroke = null;
            rect.Cursor = null;
        }

        private void Dir_MouseEnter(object sender, MouseEventArgs e)
        {
            Rectangle rect = (Rectangle)sender;
            rect.Stroke = Brushes.Orange;
            rect.Cursor = Cursors.Hand;
        }

        private void VisualizerWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        private void radioAllMoves_Checked(object sender, RoutedEventArgs e)
        {
            viewerAllMoves.Visibility = Visibility.Visible;
            viewerPredictedMoves.Visibility = Visibility.Hidden;
            SetAllMoves();
        }

        private void radioRecentMoves_Checked(object sender, RoutedEventArgs e)
        {
            viewerPredictedMoves.Visibility = Visibility.Visible;
            viewerAllMoves.Visibility = Visibility.Hidden;
            SetPredictedMoves();
        }

        public void ShowComparisonData(IEnumerable<MoveDetails> current, IEnumerable<MoveDetails> comparison)
        {
            this.lvCurrentMoves.Children.Clear();
            this.lvPreviousMoves.Children.Clear();
            ToggleComparisonMode(true);

            foreach (MoveDetails mv in current)
            {
                StackPanel pnl = new StackPanel();
                pnl.Orientation = Orientation.Horizontal;

                TextBlock txt = new TextBlock();
                txt.HorizontalAlignment = HorizontalAlignment.Left;
                txt.TextWrapping = TextWrapping.Wrap;
                txt.Text = mv.MoveNumber.ToString();
                txt.VerticalAlignment = VerticalAlignment.Top;

                Rectangle dir = new Rectangle();
                dir.Width = 20;
                dir.Height = 20;
                dir.Cursor = Cursors.Hand;
                dir.MouseEnter += Dir_MouseEnter;
                dir.MouseLeave += Dir_MouseLeave;
                dir.Margin = new Thickness(20, 0, 0, 5);

                if (mv.Direction == 0) //up
                {
                    dir.Fill = new ImageBrush { ImageSource = new BitmapImage(new Uri(System.IO.Path.Combine("Images", "arrow-up.png"), UriKind.Relative)) };
                }
                else if (mv.Direction == 1) //right
                {
                    dir.Fill = new ImageBrush { ImageSource = new BitmapImage(new Uri(System.IO.Path.Combine("Images", "arrow-right.png"), UriKind.Relative)) };
                }
                else if (mv.Direction == 2) //down
                {
                    dir.Fill = new ImageBrush { ImageSource = new BitmapImage(new Uri(System.IO.Path.Combine("Images", "arrow-down.png"), UriKind.Relative)) };
                }
                else if (mv.Direction == 3) //left
                {
                    dir.Fill = new ImageBrush { ImageSource = new BitmapImage(new Uri(System.IO.Path.Combine("Images", "arrow-left.png"), UriKind.Relative)) };
                }

                dir.HorizontalAlignment = HorizontalAlignment.Stretch;
                dir.VerticalAlignment = VerticalAlignment.Top;
                dir.Stretch = Stretch.Uniform;
                dir.InputBindings.Add(new MouseBinding()
                {
                    Gesture = new MouseGesture(MouseAction.LeftClick),
                    Command = new ItemClickedCommand(() =>
                    {
                        OnArrowClick(mv);
                    })
                });

                pnl.Children.Add(txt);
                pnl.Children.Add(dir);

                lvCurrentMoves.Children.Add(pnl);
            }

            foreach (MoveDetails mv in comparison)
            {
                StackPanel pnl = new StackPanel();
                pnl.Orientation = Orientation.Horizontal;

                TextBlock txt = new TextBlock();
                txt.HorizontalAlignment = HorizontalAlignment.Left;
                txt.TextWrapping = TextWrapping.Wrap;
                txt.Text = mv.MoveNumber.ToString();
                txt.VerticalAlignment = VerticalAlignment.Top;

                Rectangle dir = new Rectangle();
                dir.Width = 20;
                dir.Height = 20;
                dir.Cursor = Cursors.Hand;
                dir.MouseEnter += Dir_MouseEnter;
                dir.MouseLeave += Dir_MouseLeave;
                dir.Margin = new Thickness(20, 0, 0, 5);

                if (mv.Direction == 0) //up
                {
                    dir.Fill = new ImageBrush { ImageSource = new BitmapImage(new Uri(System.IO.Path.Combine("Images", "arrow-up.png"), UriKind.Relative)) };
                }
                else if (mv.Direction == 1) //right
                {
                    dir.Fill = new ImageBrush { ImageSource = new BitmapImage(new Uri(System.IO.Path.Combine("Images", "arrow-right.png"), UriKind.Relative)) };
                }
                else if (mv.Direction == 2) //down
                {
                    dir.Fill = new ImageBrush { ImageSource = new BitmapImage(new Uri(System.IO.Path.Combine("Images", "arrow-down.png"), UriKind.Relative)) };
                }
                else if (mv.Direction == 3) //left
                {
                    dir.Fill = new ImageBrush { ImageSource = new BitmapImage(new Uri(System.IO.Path.Combine("Images", "arrow-left.png"), UriKind.Relative)) };
                }

                dir.HorizontalAlignment = HorizontalAlignment.Stretch;
                dir.VerticalAlignment = VerticalAlignment.Top;
                dir.Stretch = Stretch.Uniform;
                dir.InputBindings.Add(new MouseBinding()
                {
                    Gesture = new MouseGesture(MouseAction.LeftClick),
                    Command = new ItemClickedCommand(() =>
                    {
                        OnArrowClick(mv);
                    })
                });

                pnl.Children.Add(txt);
                pnl.Children.Add(dir);

                lvPreviousMoves.Children.Add(pnl);
            }
        }

        private void CloseComparison()
        {
            this.visualizer.CloseComparisonMode();
            ToggleComparisonMode(false);
        }

        private void btnCloseComparison_Click(object sender, RoutedEventArgs e)
        {
            CloseComparison();
        }

        private void ToggleComparisonMode(bool val)
        {
            this.gridNormalView.Visibility = Visibility.Visible;
            this.panelNormalViewHeader.Visibility = Visibility.Visible;

            this.gridComparisonView.Visibility = Visibility.Visible;
            this.panelComparisonViewHeader.Visibility = Visibility.Visible;

            if (val == true)
            {
                this.gridNormalView.Visibility = Visibility.Hidden;
                this.panelNormalViewHeader.Visibility = Visibility.Hidden;
            }
            else
            {
                this.gridComparisonView.Visibility = Visibility.Hidden;
                this.panelComparisonViewHeader.Visibility = Visibility.Hidden;
            }
        }
    }
}
