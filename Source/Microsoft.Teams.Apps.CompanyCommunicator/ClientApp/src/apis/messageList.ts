import axios from 'axios';

let baseurl = window.location.origin;

export default axios.create({
    baseURL: baseurl + '/api'
});