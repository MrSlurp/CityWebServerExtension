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
            var url = _fileSwitch == false ? "showList" : "showList2";
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

        var _internalGetActiveDistrict = function () {
            console.log("request for district ids");
            var url = "districtID="+_activeDistrictId;
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

        var _districtListChanged = function (districtIds) {
            if (!_localData)
                return true;

            for (var idx in _localData.Districts) {
                var district = _localData.Districts[idx];
                if (districtIds.indexOf(district.DistrictID) < 0)
                    return true;
            }
            return false;
        }

        var promise = $interval(function () {
            if (_LocalTest)
                _internalGetCityData();
            else {
                var districts = _internalGetDistrictIds();
                if (_districtListChanged(districts)) {
                    // if any changed occured in district list, request for full city data 
                    // <enhancement> => request only new district (not sure the perf gain would worth it)
                    _internalGetCityData();
                }
                else
                    _internalGetActiveDistrict();
            }

        }.bind(this), 3000);
        _internalGetCityData();

        return {
            getCityData: function () {
                return _localData;
            },
            registerSubscriber: function (subscriber) {
                _subscribers.push(subscriber);
            }
        }
    });
});
