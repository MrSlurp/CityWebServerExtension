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
                        storeSelectionToCookie();
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

        ///////////////////////////////////////////////////////////////////////
        // set the current active district in cityInfoService
        //
        ///////////////////////////////////////////////////////////////////////
        $scope.selectActiveDistrict = function (id)
        {
            //console.log("setting active district = " + JSON.stringify(id));
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
            storeSelectionToCookie();
        }

        ///////////////////////////////////////////////////////////////////////
        //
        ///////////////////////////////////////////////////////////////////////
        var update = function () {
            var flag = false;
            if ($scope.cityName == undefined && cityInfoService.getCityData().Name != undefined) {
                // restore old selection on first update
                flag = true;
            }

            $scope.cityName = cityInfoService.getCityData().Name;
            $scope.globalDistrict = cityInfoService.getCityData().GlobalDistrict;
            $scope.allDistrict = cityInfoService.getCityData().Districts;
            // scope data must be initialized before restoring data from cookie
            if (flag) { restoreSelectionFromCookie(); }
            updateDistrictMap();
            // update the copy of districts that are displayed in currently visible district tab only
            // (ignore not displayed tabs)
            for (var idx in $scope.DistrictSelectionsTabs) {
                // not an active tab => ignore
                if ($scope.DistrictSelectionsTabs[idx].active != true)
                    continue;

                // check in current district selection
                var selectedIndexToRemove = [];
                for (var selectedIdx in $scope.DistrictSelectionsTabs[idx].selectedDistricts) {
                    var selectedDistrict = $scope.DistrictSelectionsTabs[idx].selectedDistricts[selectedIdx];
                    // if selected district does not exist anymore 
                    var sourceDataDistrict = findDistrictById(selectedDistrict.DistrictID);
                    if (sourceDataDistrict == undefined) {
                        // add it to remove list
                        selectedIndexToRemove.push(selectedIdx);
                    }
                    else {
                        if (selectedDistrict.districtVisible != undefined && selectedDistrict.districtVisible == true) {
                            // updating each field of district independantly to avoid angular rebuilding the whole tab set
                            for (var propertyName in sourceDataDistrict) {
                                $scope.DistrictSelectionsTabs[idx].selectedDistricts[selectedIdx][propertyName] = sourceDataDistrict[propertyName];
                            }
                        }
                    }
                }
                // remove district that does not exist anymore in city
                for (var i = selectedIndexToRemove.length - 1 ; i >= 0; i--) {
                    $scope.DistrictSelectionsTabs[idx].selectedDistricts.splice(selectedIndexToRemove[i], 1);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////
        // store current user district selection in cookie
        ///////////////////////////////////////////////////////////////////////
        var findDistrictById = function (searchId) {
            for (var elem in $scope.allDistrict) {
                if ($scope.allDistrict[elem].DistrictID == searchId)
                    return $scope.allDistrict[elem];
            }
            return undefined;
        }

        ///////////////////////////////////////////////////////////////////////
        // store current user district selection in cookie
        ///////////////////////////////////////////////////////////////////////
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
            // we must clean up the district data and keep only selected district IDs
            // because district data are too big for cookie
            var storableSelection = [];
            for (var tab in $scope.DistrictSelectionsTabs) {
                var tabObj = {
                    TabTitle: $scope.DistrictSelectionsTabs[tab].TabTitle,
                    selectedDistricts: []
                };
                for (var sel in $scope.DistrictSelectionsTabs[tab].selectedDistricts)
                {
                    tabObj.selectedDistricts.push({ DistrictID: $scope.DistrictSelectionsTabs[tab].selectedDistricts[sel].DistrictID });
                }
                storableSelection.push(tabObj);

            }
            $cookies.districtSelection =JSON.stringify({
                CityName:$scope.cityName,
                DistrictSelectionsTabs:storableSelection
            });
        }

        ///////////////////////////////////////////////////////////////////////
        // restore user selection from cookie
        ///////////////////////////////////////////////////////////////////////
        var restoreSelectionFromCookie = function () {
            // always create the default structure
            // it also create the default handlers for selection change
            prepareDistrictSelectionTab();
            var previousSelection = undefined;
            try{
                previousSelection = JSON.parse($cookies['districtSelection']);
            }
            catch (ex) {
            }
            if (previousSelection == undefined) {
                // no existing cookie
                return;
            }
            if ($scope.cityName != undefined) {
                // if city name is different, cancel restore
                if (previousSelection.CityName != $scope.cityName) {
                    return;
                }

                // check selected district still exist in city data
                for (var elem in previousSelection.DistrictSelectionsTabs) {
                    for (var i = 0; i < previousSelection.DistrictSelectionsTabs[elem].selectedDistricts.length;) {
                        // note that stored structure only contains the district ids
                        var distId = previousSelection.DistrictSelectionsTabs[elem].selectedDistricts[i].DistrictID;
                        // if the stored id does not exist in city data, remove selected item
                        var origDistrict = findDistrictById(distId)
                        if (origDistrict == undefined) {
                            console.log("district with id = " + distId + " does not exist anymore");
                            previousSelection.DistrictSelectionsTabs[elem].selectedDistricts.splice(i, 1);
                            // one item have been remove, restart from begining
                            i = 0;
                            continue;
                        }
                        else {
                            // district strill exist, restore it
                            previousSelection.DistrictSelectionsTabs[elem].selectedDistricts[i] = origDistrict;
                        }
                        i++;
                    }
                }
                // all is ok, use the saved selection
                $scope.DistrictSelectionsTabs = previousSelection.DistrictSelectionsTabs;
                // if there is not selected district, set city as active district 
                if ($scope.DistrictSelectionsTabs[0].selectedDistricts.length == 0)
                    $scope.selectActiveDistrict(0);
                else
                    $scope.selectActiveDistrict($scope.DistrictSelectionsTabs[0].selectedDistricts[0].DistrictID);
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
