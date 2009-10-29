using System;
using System.Linq;
using Fohjin.DDD.BankApplication.Views;
using Fohjin.DDD.Bus;
using Fohjin.DDD.Commands;
using Fohjin.DDD.Contracts;
using Fohjin.DDD.Domain;
using Fohjin.DDD.Reporting.Dto;

namespace Fohjin.DDD.BankApplication.Presenters
{
    public class ClientDetailsPresenter : Presenter<IClientDetailsView>, IClientDetailsPresenter
    {
        private bool _createNewProcess;
        private bool _addNewAccountProcess;
        private int _editStep;
        private ClientReport _clientReport;
        private ClientDetailsReport _clientDetailsReport;
        private readonly IClientDetailsView _clientDetailsView;
        private readonly IAccountDetailsPresenter _accountDetailsPresenter;
        private readonly IPopupPresenter _popupPresenter;
        private readonly ICommandBus _bus;
        private readonly IReportingRepository _reportingRepository;

        public ClientDetailsPresenter(IClientDetailsView clientDetailsView, IAccountDetailsPresenter accountDetailsPresenter, IPopupPresenter popupPresenter, ICommandBus bus, IReportingRepository reportingRepository)
            : base(clientDetailsView)
        {
            _editStep = 0;
            _createNewProcess = false;
            _addNewAccountProcess = false;
            _clientDetailsView = clientDetailsView;
            _accountDetailsPresenter = accountDetailsPresenter;
            _popupPresenter = popupPresenter;
            _bus = bus;
            _reportingRepository = reportingRepository;
        }

        public void Display()
        {
            _createNewProcess = false;
            _clientDetailsView.DisableSaveButton();
            DisableAllMenuButtons();
            _clientDetailsView.EnableOverviewPanel();

            if (_clientReport == null)
            {
                _editStep = 1;
                _createNewProcess = true;
                _clientDetailsReport = new ClientDetailsReport(Guid.NewGuid(), string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
                ResetForm();
                _clientDetailsView.EnableClientNamePanel();
                _clientDetailsView.ShowDialog();
                return;
            }

            LoadData();

            EnableAllMenuButtons();
            _clientDetailsView.ShowDialog();
        }

        private void LoadData()
        {
            _clientDetailsReport = _reportingRepository.GetByExample<ClientDetailsReport>(new { _clientReport.Id }).FirstOrDefault();

            SetClientDetailsData();
            SetReadOnlyData();
        }

        public void SetClient(ClientReport clientReport)
        {
            _clientReport = clientReport;
        }

        public void OpenSelectedAccount()
        {
            _popupPresenter.CatchPossibleException(() =>
            {
                var client = _clientDetailsView.GetSelectedAccount();
                _accountDetailsPresenter.SetAccount(client);
                _accountDetailsPresenter.Display();
            });
        }

        public void FormElementGotChanged()
        {
            _clientDetailsView.DisableSaveButton();

            if (!FormIsValid())
                return;

            if (_createNewProcess)
            {
                _clientDetailsView.EnableSaveButton();
                return;
            }

            if (_addNewAccountProcess)
            {
                _clientDetailsView.EnableSaveButton();
                return;
            }

            if (FormHasChanged())
            {
                _clientDetailsView.EnableSaveButton();
                return;
            }
        }

        public void SaveNewClientName()
        {
            _popupPresenter.CatchPossibleException(() =>
            {
                _clientDetailsView.DisableSaveButton();
                if (_createNewProcess)
                {
                    _editStep = 2;
                    _clientDetailsReport = new ClientDetailsReport(
                        _clientDetailsReport.Id,
                        _clientDetailsView.ClientName,
                        _clientDetailsReport.Street,
                        _clientDetailsReport.StreetNumber,
                        _clientDetailsReport.PostalCode,
                        _clientDetailsReport.City,
                        _clientDetailsReport.PhoneNumber);

                    _clientDetailsView.EnableAddressPanel();
                    return;
                }

                _bus.Publish(new ChangeClientNameCommand(
                                 _clientDetailsReport.Id,
                                 _clientDetailsView.ClientName));

                _clientDetailsReport = new ClientDetailsReport(
                    _clientDetailsReport.Id,
                    _clientDetailsView.ClientName,
                    _clientDetailsReport.Street,
                    _clientDetailsReport.StreetNumber,
                    _clientDetailsReport.PostalCode,
                    _clientDetailsReport.City,
                    _clientDetailsReport.PhoneNumber);

                EnableAllMenuButtons();
                _clientDetailsView.EnableOverviewPanel();
                SystemTimer.Trigger(LoadData).In(1000);
            });
        }

        public void SaveNewAddress()
        {
            _popupPresenter.CatchPossibleException(() =>
            {
                _clientDetailsView.DisableSaveButton();
                if (_createNewProcess)
                {
                    _editStep = 3;
                    _clientDetailsReport = new ClientDetailsReport(
                        _clientDetailsReport.Id,
                        _clientDetailsReport.ClientName,
                        _clientDetailsView.Street,
                        _clientDetailsView.StreetNumber,
                        _clientDetailsView.PostalCode,
                        _clientDetailsView.City,
                        _clientDetailsReport.PhoneNumber);

                    _clientDetailsView.EnablePhoneNumberPanel();
                    return;
                }

                _bus.Publish(new MoveClientToNewAddressCommand(
                                 _clientDetailsReport.Id,
                                 _clientDetailsView.Street,
                                 _clientDetailsView.StreetNumber,
                                 _clientDetailsView.PostalCode,
                                 _clientDetailsView.City));

                _clientDetailsReport = new ClientDetailsReport(
                    _clientDetailsReport.Id,
                    _clientDetailsReport.ClientName,
                    _clientDetailsView.Street,
                    _clientDetailsView.StreetNumber,
                    _clientDetailsView.PostalCode,
                    _clientDetailsView.City,
                    _clientDetailsReport.PhoneNumber);

                EnableAllMenuButtons();
                _clientDetailsView.EnableOverviewPanel();
                SystemTimer.Trigger(LoadData).In(2000);
            });
        }

        public void SaveNewPhoneNumber()
        {
            _popupPresenter.CatchPossibleException(() =>
            {
                _clientDetailsView.DisableSaveButton();
                if (_createNewProcess)
                {
                    _editStep = 4;
                    _bus.Publish(new CreateClientCommand(
                                     Guid.NewGuid(),
                                     _clientDetailsReport.ClientName,
                                     _clientDetailsReport.Street,
                                     _clientDetailsReport.StreetNumber,
                                     _clientDetailsReport.PostalCode,
                                     _clientDetailsReport.City,
                                     _clientDetailsView.PhoneNumber));

                    _clientDetailsView.Close();
                    return;
                }

                _bus.Publish(new ChangeClientPhoneNumberCommand(
                                 _clientDetailsReport.Id,
                                 _clientDetailsView.PhoneNumber));

                _clientDetailsReport = new ClientDetailsReport(
                    _clientDetailsReport.Id,
                    _clientDetailsReport.ClientName,
                    _clientDetailsReport.Street,
                    _clientDetailsReport.StreetNumber,
                    _clientDetailsReport.PostalCode,
                    _clientDetailsReport.City,
                    _clientDetailsView.PhoneNumber);

                EnableAllMenuButtons();
                _clientDetailsView.EnableOverviewPanel();
                SystemTimer.Trigger(LoadData).In(2000);
            });
        }

        public void CreateNewAccount()
        {
            _popupPresenter.CatchPossibleException(() =>
            {
                _bus.Publish(new AddNewAccountToClientCommand(
                                 _clientDetailsReport.Id,
                                 _clientDetailsView.NewAccountName));

                _addNewAccountProcess = false;
                EnableAllMenuButtons();
                _clientDetailsView.EnableOverviewPanel();
                SystemTimer.Trigger(LoadData).In(2000);
            });
        }

        public void Cancel()
        {
            if (_createNewProcess)
            {
                _clientDetailsView.Close();
                return;
            }

            _addNewAccountProcess = false;
            EnableAllMenuButtons();
            _clientDetailsView.EnableOverviewPanel();
            _clientDetailsView.DisableSaveButton();
            SetClientDetailsData();
        }

        public void InitiateClientNameChange()
        {
            _editStep = 1;
            DisableAllMenuButtons();
            _clientDetailsView.EnableClientNamePanel();
        }

        public void InitiateClientHasMoved()
        {
            _editStep = 2;
            DisableAllMenuButtons();
            _clientDetailsView.EnableAddressPanel();
        }

        public void InitiateClientPhoneNumberChanged()
        {
            _editStep = 3;
            DisableAllMenuButtons();
            _clientDetailsView.EnablePhoneNumberPanel();
        }

        public void InitiateAddNewAccount()
        {
            _editStep = 4;
            _addNewAccountProcess = true;

            _clientDetailsView.NewAccountName = string.Empty;

            DisableAllMenuButtons();
            _clientDetailsView.EnableAddNewAccountPanel();
        }

        private void SetReadOnlyData() 
        {
            _clientDetailsView.ClientNameLabel = _clientDetailsReport.ClientName;
            _clientDetailsView.PhoneNumberLabel = _clientDetailsReport.PhoneNumber;
            _clientDetailsView.AddressLine1Label = string.Format("{0} {1}", _clientDetailsReport.Street, _clientDetailsReport.StreetNumber);
            _clientDetailsView.AddressLine2Label = string.Format("{0} {1}", _clientDetailsReport.PostalCode, _clientDetailsReport.City);
        }

        private void ResetForm() 
        {
            _clientDetailsView.ClientName = string.Empty;
            _clientDetailsView.Street = string.Empty;
            _clientDetailsView.StreetNumber = string.Empty;
            _clientDetailsView.PostalCode = string.Empty;
            _clientDetailsView.City = string.Empty;
            _clientDetailsView.PhoneNumber = string.Empty;
            _clientDetailsView.Accounts = null;
            _clientDetailsView.ClosedAccounts = null;
        }

        private void DisableAllMenuButtons() 
        {
            _clientDetailsView.DisableAddNewAccountMenu();
            _clientDetailsView.DisableClientHasMovedMenu();
            _clientDetailsView.DisableNameChangedMenu();
            _clientDetailsView.DisablePhoneNumberChangedMenu();
        }

        private void SetClientDetailsData()
        {
            _clientDetailsView.ClientName = _clientDetailsReport.ClientName;
            _clientDetailsView.Street = _clientDetailsReport.Street;
            _clientDetailsView.StreetNumber = _clientDetailsReport.StreetNumber;
            _clientDetailsView.PostalCode = _clientDetailsReport.PostalCode;
            _clientDetailsView.City = _clientDetailsReport.City;
            _clientDetailsView.PhoneNumber = _clientDetailsReport.PhoneNumber;
            _clientDetailsView.Accounts = _clientDetailsReport.Accounts;
            _clientDetailsView.ClosedAccounts = _clientDetailsReport.ClosedAccounts;
        }

        private void EnableAllMenuButtons() 
        {
            _clientDetailsView.EnableAddNewAccountMenu();
            _clientDetailsView.EnableClientHasMovedMenu();
            _clientDetailsView.EnableNameChangedMenu();
            _clientDetailsView.EnablePhoneNumberChangedMenu();
        }

        private bool FormIsValid()
        {
            if (_editStep == 0)
                return true;

            if (_editStep == 1)
                return !string.IsNullOrEmpty(_clientDetailsView.ClientName);

            if (_editStep == 2)
                return
                    !string.IsNullOrEmpty(_clientDetailsView.Street) &&
                    !string.IsNullOrEmpty(_clientDetailsView.StreetNumber) &&
                    !string.IsNullOrEmpty(_clientDetailsView.PostalCode) &&
                    !string.IsNullOrEmpty(_clientDetailsView.City);

            if (_editStep == 3)
                return !string.IsNullOrEmpty(_clientDetailsView.PhoneNumber);

            if (_editStep == 4)
                return 
                    !string.IsNullOrEmpty(_clientDetailsView.NewAccountName);

            throw new Exception("Edit step was not properly initialized!");
        }

        private bool FormHasChanged()
        {
            return
                AddressHasChanged() ||
                PhoneNumberHasChanged() ||
                ClientNameHasChanged();
        }

        private bool AddressHasChanged()
        {
            return
                _clientDetailsView.Street != _clientDetailsReport.Street ||
                _clientDetailsView.StreetNumber != _clientDetailsReport.StreetNumber ||
                _clientDetailsView.PostalCode != _clientDetailsReport.PostalCode ||
                _clientDetailsView.City != _clientDetailsReport.City;
        }

        private bool PhoneNumberHasChanged()
        {
            return _clientDetailsView.PhoneNumber != _clientDetailsReport.PhoneNumber;
        }

        private bool ClientNameHasChanged()
        {
            return _clientDetailsView.ClientName != _clientDetailsReport.ClientName;
        }
    }
}