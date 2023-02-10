<template>
    <div id="bg" class="absolute"></div>
</template>

<script>
import { onMounted } from '@vue/runtime-core';
export default {
    setup () {

        const render = () => {
            particlesJS('bg',

                {
                    "particles": {
                        "number": {
                            "value": 19,
                            "density": {
                                "enable": true,
                                "value_area": 552.4033491425909
                            }
                        },
                        "color": {
                            "value": "random"
                        },
                        "shape": {
                            "type": "circle",
                            "stroke": {
                                "width": 0,
                                "color": "random"
                            },
                            "polygon": {
                                "nb_sides": 3
                            },
                            "image": {
                                "src": "img/github.svg",
                                "width": 100,
                                "height": 100
                            }
                        },
                        "opacity": {
                            "value": 1,
                            "random": false,
                            "anim": {
                                "enable": true,
                                "speed": 0,
                                "opacity_min": 0,
                                "sync": true
                            }
                        },
                        "size": {
                            "value": 7.891476416322726,
                            "random": true,
                            "anim": {
                                "enable": false,
                                "speed": 0,
                                "size_min": 2.436231636904035,
                                "sync": false
                            }
                        },
                        "line_linked": {
                            "enable": true,
                            "distance": 150,
                            "color": "#ffffff",
                            "opacity": 0.4,
                            "width": 1
                        },
                        "move": {
                            "enable": true,
                            "speed": 3.1565905665290903,
                            "direction": "none",
                            "random": true,
                            "straight": false,
                            "out_mode": "bounce",
                            "bounce": false,
                            "attract": {
                                "enable": false,
                                "rotateX": 600,
                                "rotateY": 1200
                            }
                        }
                    },
                    "interactivity": {
                        "detect_on": "canvas",
                        "events": {
                            "onhover": {
                                "enable": false,
                                "mode": "repulse"
                            },
                            "onclick": {
                                "enable": false,
                                "mode": "push"
                            },
                            "resize": true
                        },
                        "modes": {
                            "grab": {
                                "distance": 400,
                                "line_linked": {
                                    "opacity": 0
                                }
                            },
                            "bubble": {
                                "distance": 24.362316369040357,
                                "size": 0,
                                "duration": 0,
                                "opacity": 8,
                                "speed": 3
                            },
                            "repulse": {
                                "distance": 0,
                                "duration": 0.4
                            },
                            "push": {
                                "particles_nb": 4
                            },
                            "remove": {
                                "particles_nb": 2
                            }
                        }
                    },
                    "retina_detect": false
                }

            );
        }
        onMounted(render);


        return {}
    }
}
</script>