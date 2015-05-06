'use strict';

define([
    'app/components/city/module'
], function (module) {
    module.factory('cityInfoService', function ($http, $interval) {
        console.log("cityInfoService ready for duty");
        var _LocalTest = false;
        var _fileSwitch = false;
        var _localData = {};
        var _subscribers = [];

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
        
        var promise = $interval(function () {
            _internalGetCityData();
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
