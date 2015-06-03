'use strict';

define([
    'app/components/district/module',
    'app/components/dataServices/cityInfoService',
], function (module) {
    module.controller('DistrictCtrl', function ($scope, cityInfoService, $interval, $cookies) {
        console.log("DistrictCtrl Controller reporting for duty.");

        // contains the data of city and district retrieved from cityInfoService
        $scope.cityName = undefined;
        $scope.globalDistrict = undefined;
        $scope.allDistrict = undefined

        // district map contains the list of all districts by name
        // it is used by selection drop down box
        $scope.DistricMap = {};
        // contains as many object as primary panel tab
        // each object contains data about currently selected districts
        // in each tab.
        $scope.DistrictSelectionsTabs = {};

        // select combobox settings
        // all combobox uses the same settings and the same data source (city DistricMap)
        // but use a different selected item list
        $scope.selectBoxSettings = {
            idProp: 'DistrictID',
            displayProp: 'DistrictName',
            externalIdProp: '',
            scrollable: true,
            scrollableHeight: 450,
        };


        ///////////////////////////////////////////////////////////////////////
        // create a new $scope.DistrictSelectionsTabs content
        ///////////////////////////////////////////////////////////////////////
        var prepareDistrictSelectionTab = function () {
            $scope.DistrictSelectionsTabs = {};
            // creating the object list of district tabs
            for (var i = 0; i < 6; i++) {
                var PrepareTab = function () {
                    // base structure
                    var districtSelectionTab = {
                        TabTitle: "No Selection",
                        selectedDistricts: []
                    };

                    var selectionChange = function () {
                        var nbDistrictSelected = districtSelectionTab.selectedDistricts.length;
                        districtSelectionTab.TabTitle = nbDistrictSelected == 0 ? "No Selection" : (nbDistrictSelected + " Selected");
                        $scope.selectActiveDistrict(nbDistrictSelected == 0 ? 0 : districtSelectionTab.selectedDistricts[0].DistrictID);
                    }
                    // adding events
                    districtSelectionTab.events = {
                        onItemSelect: selectionChange,
                        onItemDeselect: selectionChange,
                        onSelectAll: selectionChange,
                        onUnselectAll: selectionChange,
                    };
                    return districtSelectionTab;
                }

                $scope.DistrictSelectionsTabs[i] = PrepareTab();
            }
        }

        //prepareDistrictSelectionTab();

        ///////////////////////////////////////////////////////////////////////
        // set the current active district in cityInfoService
        //
        ///////////////////////////////////////////////////////////////////////
        $scope.selectActiveDistrict = function (id)
        {
            console.log("setting active district = " + JSON.stringify(id));
            cityInfoService.setActiveDistrictId(id);
        }

        ///////////////////////////////////////////////////////////////////////
        //
        ///////////////////////////////////////////////////////////////////////
        $scope.selectMostlyOfCategory = function (districtSelectionsTab, categoryName) {
            var getMostPresentTypeFromService = function (service) {
                var greatestValue = 0;
                var greatestCat="";
                for (var buildingcat in service.Categories) {
                    if (service.Categories[buildingcat].Current > greatestValue) {
                        greatestValue = service.Categories[buildingcat].Current;
                        greatestCat = district.Consumptions.Building.Categories[buildingcat].Name;
                    }
                }
                return greatestCat;
            }
            districtSelectionsTab.selectedDistricts = [];
            for (var districtIdx in $scope.allDistrict) {
                var district = $scope.allDistrict[districtIdx];
                if (getMostPresentTypeFromService(district.Consumptions.Building) == categoryName){
                    districtSelectionsTab.selectedDistricts.push(district);
                }
            }
            $scope.selectActiveDistrict(districtSelectionsTab.length == 0 ? 0 : districtSelectionsTab.selectedDistricts[0].DistrictID);
            districtSelectionsTab.TabTitle = "Mostly " + categoryName;
        }

        ///////////////////////////////////////////////////////////////////////
        //
        ///////////////////////////////////////////////////////////////////////
        var update = function () {
            $scope.cityName = cityInfoService.getCityData().Name;
            $scope.globalDistrict = cityInfoService.getCityData().GlobalDistrict;
            $scope.allDistrict = cityInfoService.getCityData().Districts;
            for (var elem in $scope.allDistrict) {
                // create the entry with the name as key
                $scope.DistricMap[$scope.allDistrict[elem].DistrictName] = $scope.allDistrict[elem];

                //also update the copy if districts that are displayed
                // in currently display district tab only
                // (ignore not displayed tabs)
                for (var idx in $scope.DistrictSelectionsTabs) {
                    if ($scope.DistrictSelectionsTabs[idx].active != true)
                        continue;
                    // check in current district selection
                    for (var selectedIdx in $scope.DistrictSelectionsTabs[idx].selectedDistricts) {
                        // is current district is the same 
                        if ($scope.DistrictSelectionsTabs[idx].selectedDistricts[selectedIdx].DistrictID == $scope.allDistrict[elem].DistrictID) {
                            if ($scope.DistrictSelectionsTabs[idx].selectedDistricts[selectedIdx].districtVisible != undefined
                                && $scope.DistrictSelectionsTabs[idx].selectedDistricts[selectedIdx].districtVisible == true) {
                                // updating each field of district independantly to avoid angular rebuilding the whole tab set
                                for (var propertyName in $scope.allDistrict[elem]) {
                                    $scope.DistrictSelectionsTabs[idx].selectedDistricts[selectedIdx][propertyName] = $scope.allDistrict[elem][propertyName];
                                }
                            }
                        }
                    }
                }
            }
        }

        var updateDistrictMap = function () {
            // first remove elements that are not present anymore
            for (var key in $scope.DistricMap) {
                var found = false;
                var name = key;
                for (var elem in $scope.allDistrict) {
                    if ($scope.allDistrict[elem].DistrictName == name) {
                        found = true;
                        break;
                    }
                }
                if (!found) {
                    delete $scope.DistricMap[name];
                }
            }

            for (var elem in $scope.allDistrict) {
                $scope.DistricMap[$scope.allDistrict[elem].DistrictName] = $scope.allDistrict[elem];
            }

        }

        ///////////////////////////////////////////////////////////////////////
        // store current user district selection in cookie
        ///////////////////////////////////////////////////////////////////////
        var storeSelectionToCookie = function () {
            $cookies.putObject({
                CityName:$scope.cityName,
                DistrictSelectionsTabs:$scope.DistrictSelectionsTabs
            });
        }

        ///////////////////////////////////////////////////////////////////////
        // restore user selection from cookie
        ///////////////////////////////////////////////////////////////////////
        var restoreSelectionFromCookie = function () {
            var previousSelection = $cookies.get('districtSelection');
            if ($scope.cityName != undefined){
                if (previousSelection.CityName != $scope.cityName) {
                    prepareDistrictSelectionTab();
                    return;
                }
                // check the district list is the same
            
                // all is ok, use the save selection

                // else, try to merge

                // else reset selection
            }
        }

        ///////////////////////////////////////////////////////////////////////
        // used for auto sizing panel in dynamic view
        // @param : first argument is the size factor to apply for bootstrap column size
        // @param : other arguments should be serviceDataObject or boolean
        ///////////////////////////////////////////////////////////////////////
        $scope.SmartColumnLayoutPanel = function () {
            if (arguments.length < 2)
                return 24;
            var coeff = arguments[0];
            var count = 0;
            for (var index = 1 ; index < arguments.length; index++) {
                if ($scope.IsServiceInfoValuable(arguments[index]))
                    count++;
            }
            return count*coeff;
        }

        ///////////////////////////////////////////////////////////////////////
        // used for auto sizing elements in dynamic panels
        // @params : (...) arguments must be serviceData Object or boolean
        ///////////////////////////////////////////////////////////////////////
        $scope.SmartColumnLayoutElement = function () {
            var count = 0;
            for (var index = 0 ; index < arguments.length; index++) {
                if ($scope.IsServiceInfoValuable(arguments[index]))
                    count++;
            }
            if (count == 0)
                return 1;
            return parseInt(24 / count);
        }

        ///////////////////////////////////////////////////////////////////////
        // return true if service data is defined and its values are valuables
        ///////////////////////////////////////////////////////////////////////
        $scope.IsServiceInfoValuable = function (serviceData)
        {
            if (typeof (serviceData) == "boolean") {
                return serviceData;
            }
            if (serviceData.TotalCurrent != 0)
                return true;
            return false;
        }

        ///////////////////////////////////////////////////////////////////////
        // return true if service data is defined and its values are valuables
        // @params : (...) arguments must be serviceData Object or boolean
        ///////////////////////////////////////////////////////////////////////
        $scope.IsPanelInfoValuable = function () //(...)
        {
            // arguments are services datas
            for (var index in arguments) {
                if ($scope.IsServiceInfoValuable(arguments[index]))
                    return true;
            }
            return false;
        }

        // register itself in cityInfoService in order to update
        // when data are retrieved
        cityInfoService.registerSubscriber(function () {
            update();
        });
    });
});
