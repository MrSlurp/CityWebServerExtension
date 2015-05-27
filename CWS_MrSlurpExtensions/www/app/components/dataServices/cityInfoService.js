'use strict';

define([
    'app/components/city/module'
], function (module) {
    module.factory('cityInfoService', function ($http, $interval) {
        console.log("cityInfoService ready for duty");
        var _LocalTest = location.port == 60666? true : false;
        var _fileSwitch = false;
        var _localData = {};
        var _subscribers = [];
        var _activeDistrictId = 0;

        var _internalGetCityData = function () {
            console.log("request for city data");
            var url = _fileSwitch == false ? "CityInfo" : "CityInfo2";
            if (_LocalTest == true) {
                url = "SlurpUI/" + url;
                _fileSwitch = !_fileSwitch;
            }
            $http.get(url).success(function (response) {
                _localData = response;
                for (var idx in _subscribers) {
                    _subscribers[idx]();
                }
            });
        }

        var _internalGetDistrictIds = function () {
            console.log("request for district ids");
            var url = "CityInfo?showList=";
            $http.get(url).success(function (response) {
                if (_districtListChanged(response)) {
                    // if any change occured in district list, request for full city data 
                    // <enhancement> => request only new district (not sure the perf gain would worth it)
                    _internalGetCityData();
                }
                else
                    _internalGetActiveDistrict();
            });
        }

        var getDistrictIndex = function (districtId) {
            if (!_localData)
                return 0;
            for (var idx in _localData.Districts) {
                var district = _localData.Districts[idx];
                if (district.DistrictID == districtId)
                    return idx;
            }
            return 0;
        }

        var _internalGetActiveDistrict = function () {
            console.log("request for district ids");
            var url = "CityInfo?districtID=" + _activeDistrictId;
            $http.get(url).success(function (response) {
                if (response.GlobalDistrict != null) {
                    _localData.GlobalDistrict = response.GlobalDistrict;
                }
                for (var index in response.Districts) {
                    console.log("updating district id = "+ response.Districts[index].DistrictID);
                    var dindex = getDistrictIndex(response.Districts[index].DistrictID);
                    _localData.Districts[dindex] = response.Districts[index];
                }
                for (var idx in _subscribers) {
                    _subscribers[idx]();
                }
            });

        }

        var _districtListChanged = function (districtIds) {
            if (!_localData || !districtIds)
                return true;

            for (var idx in _localData.Districts) {
                var district = _localData.Districts[idx];
                if (districtIds.indexOf(district.DistrictID) < 0)
                    return true;
            }
            return false;
        }

        var promise = $interval(function () {
            console.log("active district Id = " + _activeDistrictId);
            if (_LocalTest)
                _internalGetCityData();
            else {
                _internalGetDistrictIds();
            }

        }.bind(this), 3000);
        _internalGetCityData();

        return {
            getCityData: function () {
                return _localData;
            },
            registerSubscriber: function (subscriber) {
                _subscribers.push(subscriber);
            },
            setActiveDistrictId: function (id) {
                _activeDistrictId = id;
            }
        }
    });
});
