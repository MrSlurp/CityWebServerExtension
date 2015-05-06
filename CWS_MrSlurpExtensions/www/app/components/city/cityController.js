'use strict';

define([
    'app/components/city/module',
    'app/components/dataServices/cityInfoService'
], function (module) {
    module.controller('CityCtrl', function ($scope, cityInfoService, $interval) {
        console.log("City Controller reporting for duty.");
        var update = function () {
            $scope.CityData = cityInfoService.getCityData();
            $scope.CityName = $scope.CityData.Name;
        }
        cityInfoService.registerSubscriber(function () {
            update();
        });
        update();
    });
});
