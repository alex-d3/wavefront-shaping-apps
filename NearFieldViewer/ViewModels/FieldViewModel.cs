using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ScatLib;
using NearFieldViewer.Models;
using Microsoft.Win32;
using System.IO;
using System.Windows.Controls;
using System.Globalization;
//using System.Drawing;
//using System.Drawing.Imaging;

namespace NearFieldViewer.ViewModels
{
    public enum UnitOfLength { Arbitrary, Micrometer }
    public enum Scale { Linear, Mu }

    public interface IFieldViewModel : INotifyPropertyChanged
    {
        void Update();
    }
    public class FieldViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        private readonly FieldModel _model;

        private string _name;

        private WriteableBitmap _bmpField;
        private double _displayRangeMin;
        private double _displayRangeMax;

        private int roi_x, roi_y, roi_w, roi_h;

        private int _nodesX;
        private int _nodesY;
        private double _minX;
        private double _minY;
        private double _maxX;
        private double _maxY;
        private double _stepX;
        private double _stepY;
        private double _wavelength;
        private double _energy;
        private double _intensityMax;

        private Scale _scale;
        private List<Scale> _scales;
        private double MU = 255.0;

        public ModelCommand RedrawCommand { get; private set; }
        public ModelCommand LoadFieldCommand { get; private set; }
        public ModelCommand ResetImageCommand { get; private set; }

        public FieldViewModel(FieldModel model)
        {
            _model = model;
            LoadFieldCommand = new ModelCommand(param => LoadField());
            RedrawCommand = new ModelCommand(param => RedrawImage());
            ResetImageCommand = new ModelCommand(exe => ResetImage(), canExe => CanResetImage());

            _scales = new List<Scale>(2);
            _scales.Add(Scale.Linear);
            _scales.Add(Scale.Mu);
            _scale = _scales.FirstOrDefault();
        }

        public FieldViewModel()
        {
        }

        unsafe public void RedrawImage()
        {
            byte* raw;
            int stride;
            int bytesPerPixel;
            byte pixel;
            Func<int, int, byte> px;

            if (_bmpField == null)
                _bmpField = new WriteableBitmap(NodesX, NodesY, 96, 96, System.Windows.Media.PixelFormats.Bgr24, null);

            _bmpField.Lock();

            raw = (byte*)_bmpField.BackBuffer;
            stride = _bmpField.BackBufferStride;
            bytesPerPixel = _bmpField.Format.BitsPerPixel / 8;

            switch (_scale)
            {
                case Scale.Linear:
                    px = LinearPixel;
                    break;
                case Scale.Mu:
                    px = MuLawPixel;
                    break;
                default:
                    px = LinearPixel;
                    break;
            }

            for (int y = NodesY - 1, offset = 0; y >= 0; y--)
            {
                for (int x = 0; x < NodesX; x++)
                {
                    if (_model.Field[x, y] < _displayRangeMin)
                    {
                        raw[offset++] = 148; // Blue
                        raw[offset++] = 86;  // Green
                        raw[offset++] = 44;  // Red
                    }
                    else if (_model.Field[x, y] > _displayRangeMax)
                    {
                        raw[offset++] = 59;  // Blue
                        raw[offset++] = 64;  // Green
                        raw[offset++] = 148; // Red
                    }
                    else
                    {
                        pixel = px(x, y);
                        //pixel = (byte)((_model.Field[x, y] - _displayRangeMin) / (_displayRangeMax - _displayRangeMin) * byte.MaxValue);
                        raw[offset++] = pixel; // Blue
                        raw[offset++] = pixel; // Green
                        raw[offset++] = pixel; // Red
                    }
                }
                offset += stride - NodesX * bytesPerPixel;
            }

            _bmpField.AddDirtyRect(new System.Windows.Int32Rect(0, 0, _bmpField.PixelWidth, _bmpField.PixelHeight));
            _bmpField.Unlock();
            OnPropertyChanged("Image");

            using (FileStream fs = new FileStream("im.png", FileMode.Create))
            {
                PngBitmapEncoder enc = new PngBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(_bmpField));
                enc.Save(fs);
            }
        }

        private byte LinearPixel(int x, int y)
        {
            return (byte)((_model.Field[x, y] - _displayRangeMin) / (_displayRangeMax - _displayRangeMin) * byte.MaxValue);
        }

        private byte MuLawPixel(int x, int y)
        {
            return (byte)(
                Math.Log(1.0 + MU * (_model.Field[x, y] - _displayRangeMin) / (_displayRangeMax - _displayRangeMin)) /
                Math.Log(1.0 + MU) * byte.MaxValue);
        }

        public void LoadField()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Binary (*.bin)|*.bin|Comma separated (*.csv)|*.csv";
            if (ofd.ShowDialog() == true)
            {
                if (_model.Field != null)
                    _model.Field.Dispose();
                _model.Field = new NearField(ofd.FileName);

                MinX = _model.Field.MinX;
                MaxX = _model.Field.MaxX;
                StepX = _model.Field.StepX;
                MinY = _model.Field.MinY;
                MaxY = _model.Field.MaxY;
                StepY = _model.Field.StepY;
                Wavelength = _model.Field.Wavelength;
                NodesX = _model.Field.NodesX;
                NodesY = _model.Field.NodesY;
                Name = Path.GetFileName(ofd.FileName);
                Energy = _model.Field.ElectricFieldEnergy;
                _intensityMax = _model.Field.GetMaxIntensity();
                DisplayRangeMin = 0.0;
                DisplayRangeMax = _intensityMax;
                Image = null;
                RedrawImage();

                ResetImageCommand.OnCanExecuteChanged();
            }
        }

        public void ResetImage()
        {
            DisplayRangeMin = 0.0;
            DisplayRangeMax = _intensityMax;
            RedrawImage();
        }

        public bool CanResetImage()
        {
            if (_model == null || _model.Field == null)
                return false;

            return true;
        }

        #region Properties
        public string Name
        {
            get
            {
                return _name;
            }
            private set
            {
                _name = value;
                OnPropertyChanged("Name");
            }
        }

        public WriteableBitmap Image
        {
            get
            {
                if (_bmpField == null)
                    return null;
                
                return _bmpField;
            }
            private set
            {
                _bmpField = value;
                OnPropertyChanged("Image");
            }
        }

        public double DisplayRangeMin
        {
            get
            {
                return _displayRangeMin;
            }
            set
            {
                _displayRangeMin = value;
                OnPropertyChanged("DisplayRangeMin");
            }
        }

        public double DisplayRangeMax
        {
            get
            {
                return _displayRangeMax;
            }
            set
            {
                _displayRangeMax = value;
                OnPropertyChanged("DisplayRangeMax");
            }
        }

        public double MinX
        {
            get
            {
                return _minX;
            }
            private set
            {
                _minX = value;
                OnPropertyChanged("MinX");
            }
        }
        public double MaxX
        {
            get
            {
                return _maxX;
            }
            private set
            {
                _maxX = value;
                OnPropertyChanged("MaxX");
            }
        }

        public double StepX
        {
            get
            {
                return _stepX;
            }
            private set
            {
                _stepX = value;
                OnPropertyChanged("StepX");
            }
        }

        public double MinY
        {
            get
            {
                return _minY;
            }
            private set
            {
                _minY = value;
                OnPropertyChanged("MinY");
            }
        }

        public double MaxY
        {
            get
            {
                return _maxY;
            }
            private set
            {
                _maxY = value;
                OnPropertyChanged("MaxY");
            }
        }

        public double StepY
        {
            get
            {
                return _stepY;
            }
            private set
            {
                _stepY = value;
                OnPropertyChanged("StepY");
            }
        }

        public int NodesX
        {
            get
            {
                return _nodesX;
            }
            private set
            {
                _nodesX = value;
                OnPropertyChanged("NodesX");
            }
        }

        public int NodesY
        {
            get
            {
                return _nodesY;
            }
            private set
            {
                _nodesY = value;
                OnPropertyChanged("NodesY");
            }
        }

        public double Wavelength
        {
            get
            {
                return _wavelength;
            }
            private set
            {
                _wavelength = value;
                OnPropertyChanged("Wavelength");
            }
        }

        public double Energy
        {
            get
            {
                return _energy;
            }
            private set
            {
                _energy = value;
                OnPropertyChanged("Energy");
            }
        }

        public Scale SelectedScale
        {
            get
            {
                return _scale;
            }
            set
            {
                _scale = value;
                OnPropertyChanged("SelectedScale");

                RedrawImage();
            }
        }

        public List<Scale> Scales
        {
            get
            {
                return _scales;
            }
        }

        public int RoiX
        {
            get
            {
                return roi_x;
            }
            set
            {
                roi_x = value;
                OnPropertyChanged("RoiX");
            }
        }

        public int RoiY
        {
            get
            {
                return roi_y;
            }
            set
            {
                roi_y = value;
                OnPropertyChanged("RoiY");
            }
        }

        public int RoiWidth
        {
            get
            {
                return roi_w;
            }
            set
            {
                roi_w = value;
                OnPropertyChanged("RoiWidth");
            }
        }

        public int RoiHeight
        {
            get
            {
                return roi_h;
            }
            set
            {
                roi_h = value;
                OnPropertyChanged("RoiHeight");
            }
        }
        #endregion
    }

    public class ModelCommand : ICommand
    {
        private readonly Action<object> _execute = null;
        private readonly Predicate<object> _canExecute = null;

        public ModelCommand(Action<object> execute)
            : this(execute, null)
        {
        }

        public ModelCommand(Action<object> execute, Predicate<object> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return _canExecute != null ? _canExecute(parameter) : true;
        }

        public void Execute(object parameter)
        {
            if (_execute != null)
                _execute(parameter);
        }

        public void OnCanExecuteChanged()
        {
            CanExecuteChanged(this, EventArgs.Empty);
        }
    }

    //public class RangeValidationRule<T> : ValidationRule
    //    where T : IComparable
    //{
    //    public T Min { get; set; }
    //    public T Max { get; set; }

    //    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    //    {
    //        T parameter = 0.0;

    //        try
    //        {
    //            string str = value as string;
    //            if (!string.IsNullOrEmpty(str))
    //            {
    //                parameter = T.Parse(str);
    //            }
    //        }
    //        catch (Exception e)
    //        {
    //            return new ValidationResult(false, "Illegal characters or " + e.Message);
    //        }

    //        if (parameter < this.Min || parameter > this.Max)

    //        //throw new NotImplementedException();
    //    }
    //}
}
