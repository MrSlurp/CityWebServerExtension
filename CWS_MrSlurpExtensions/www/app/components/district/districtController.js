﻿'use strict';

define([
    'app/components/district/module',
    'app/components/dataServices/cityInfoService'
], function (module) {
    module.controller('DistrictCtrl', function ($scope, cityInfoService, $interval) {
        console.log("DistrictCtrl Controller reporting for duty.");

        $scope.DistricMap = {};

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
        // select combobox settings
        // all combobox uses the same settings and the same data source (city districts)
        // but use a different selected item list
        $scope.selectBoxSettings = {
            idProp: 'DistrictID', 
            displayProp: 'DistrictName', 
            externalIdProp: '',
            scrollable: true,
            scrollableHeight:450,
        };

        var firstUpdateDone = false;
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
            districtSelectionsTab.TabTitle = "Mostly " + categoryName;
        }

        var update = function () {
            $scope.globalDistrict = cityInfoService.getCityData().GlobalDistrict;
            $scope.allDistrict = cityInfoService.getCityData().Districts
            for (var elem in $scope.allDistrict) {
                // create the entry as 
                $scope.DistricMap[$scope.allDistrict[elem].DistrictName] = $scope.allDistrict[elem];
                //also update the copy if districts that are displayed
                for (var idx in $scope.DistrictSelectionsTabs) {
                    if ($scope.DistrictSelectionsTabs[idx].active != true)
                        continue;
                    for (var selectedIdx in $scope.DistrictSelectionsTabs[idx].selectedDistricts) {
                        if ($scope.DistrictSelectionsTabs[idx].selectedDistricts[selectedIdx].DistrictName == $scope.allDistrict[elem].DistrictName) {
                            // updating each field of district independantly to avoid angular rebuilding the whole tab set
                            if ($scope.DistrictSelectionsTabs[idx].selectedDistricts[selectedIdx].districtVisible != undefined
                                && $scope.DistrictSelectionsTabs[idx].selectedDistricts[selectedIdx].districtVisible == true) {
                                for (var propertyName in $scope.allDistrict[elem]) {
                                    $scope.DistrictSelectionsTabs[idx].selectedDistricts[selectedIdx][propertyName] = $scope.allDistrict[elem][propertyName];
                                }
                            }
                        }
                    }
                }
            }
        }

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

        $scope.IsServiceInfoValuable = function(serviceData)
        {
            if (typeof (serviceData) == "boolean") {
                return serviceData;
            }
            if (serviceData.TotalCurrent != 0)
                return true;
            return false;
        }

        $scope.IsPanelInfoValuable = function() //(...)
        {
            // arguments are services datas
            for (var index in arguments) {
                if ($scope.IsServiceInfoValuable(arguments[index]))
                    return true;
            }
            return false;
        }


        // in case of district view
        cityInfoService.registerSubscriber(function () {
            update();
        });
    });
});
